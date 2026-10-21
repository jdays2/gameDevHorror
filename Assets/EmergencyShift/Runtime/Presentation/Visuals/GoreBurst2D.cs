using System.Collections;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Одноразовый разрыв тела. Лужа и куски независимы от игрового поведения врага.</summary>
    public sealed class GoreBurst2D : MonoBehaviour
    {
        private GoreSpawnBudget2D spawnBudget;

        [SerializeField]
        private BloodPool2D bloodPoolPrefab;
        [SerializeField]
        private Sprite[] fragments;
        [SerializeField]
        private Material litMaterial;
        [SerializeField]
        private Sprite bloodSpraySprite;
        [SerializeField]
        private Sprite contactShadowSprite;
        [SerializeField]
        private Material shadowMaterial;
        [SerializeField]
        private GoreProfile profile;
        [SerializeField, Tooltip("Original death-animation body, hidden when an explicit corpse replaces it.")]
        private SpriteRenderer replacedBody;
        private GoreVariant variant;
        private Transform remainsRoot;
        private RemainsRecord2D record;
        private bool spawning;
        private bool emitted;

        public bool IsSettled => !spawning && (record == null || record.IsSettled);
        public event System.Action<GibMotion2D> FragmentLaunched;

        [Inject]
        private void Construct(GoreSpawnBudget2D budget) => spawnBudget = budget;

        public void Configure(BloodPool2D pool, Sprite[] pieces, Material material)
        {
            bloodPoolPrefab = pool;
            fragments = pieces;
            litMaterial = material;
        }

        public void ConfigureRupture(Sprite blood, Sprite shadow, Material material)
        {
            bloodSpraySprite = blood;
            contactShadowSprite = shadow;
            shadowMaterial = material;
        }

        public void Play(LayerMask obstacles)
        {
            if (emitted)
            {
                return;
            }

            if (profile == null || profile.variants == null || profile.variants.Length == 0)
            {
                Debug.LogError("Missing gore profile: " + name, this);
                return;
            }

            emitted = true;
            variant = profile.ChooseVariant();
            if (variant.pieces != null)
            {
                foreach (GorePiece part in variant.pieces)
                {
                    if (part == null || fragments == null || part.spriteIndex < 0 || part.spriteIndex >= fragments.Length || fragments[part.spriteIndex] == null)
                    {
                        Debug.LogError("Invalid anatomical gore composition: " + name, this);
                        return;
                    }
                }
            }
            CreateRemainsRoot(transform.position);
            spawning = true;
            // Кровь появляется сразу при переходе в Dead; разлёт частей следует за разрывом.
            if (bloodPoolPrefab != null)
            {
                BloodPool2D pool = Instantiate(bloodPoolPrefab, new Vector3(transform.position.x, transform.position.y, -0.02f), Quaternion.identity);
                pool.name = "Blood pool";
                pool.transform.SetParent(remainsRoot, true);
                record.RegisterPool(pool);
                pool.Initialize(profile.poolSize * Random.Range(0.85f, 1.18f), Random.Range(1, int.MaxValue), obstacles);
                pool.BeginRupture();
                if (bloodSpraySprite != null)
                {
                    var spray = new GameObject("Blood rupture burst", typeof(BloodRuptureBurst2D));
                    spray.transform.position = new Vector3(transform.position.x, transform.position.y, -.034f);
                    spray.GetComponent<BloodRuptureBurst2D>().Initialize(pool, bloodSpraySprite, litMaterial);
                }
            }

            StartCoroutine(Burst(obstacles));
        }

        private IEnumerator Burst(LayerMask obstacles)
        {
            // Первые кадры показывают разрыв, затем отсоединённые части получают своё движение.
            yield return new WaitForSeconds(profile.releaseDelay);
            if (fragments == null || fragments.Length == 0)
            {
                spawning = false;
                yield break;
            }

            bool explicitComposition = variant.pieces != null && variant.pieces.Length > 0;
            int count = explicitComposition ? variant.pieces.Length : variant.fragmentCount;
            if (variant.replaceOriginalBody && replacedBody != null)
            {
                replacedBody.enabled = false;
            }

            for (int i = 0; i < count; i++)
            {
                while (!spawnBudget.TryReserve())
                {
                    yield return null;
                }

                GorePiece specification = explicitComposition ? variant.pieces[i] : null;
                int spriteIndex = specification != null ? specification.spriteIndex : (i + variant.spriteOffset) % fragments.Length;
                Vector2 scaleRange = specification != null ? specification.scale : variant.scale;
                var piece = new GameObject("Creature fragment", typeof(GibMotion2D));
                piece.transform.position = new Vector3(transform.position.x, transform.position.y, RemainsVisualStyle.SurfaceZ);
                if (specification != null)
                {
                    piece.transform.position += transform.TransformVector(specification.offset);
                }
                piece.transform.SetParent(remainsRoot, true);
                var body = new GameObject("Fragment visual", typeof(SpriteRenderer));
                body.transform.SetParent(piece.transform, false);
                body.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
                var renderer = body.GetComponent<SpriteRenderer>();
                renderer.sprite = fragments[spriteIndex];
                renderer.sharedMaterial = litMaterial;
                renderer.sortingOrder = RemainsVisualStyle.DeadOrder;
                renderer.color = RemainsVisualStyle.Tint;
                float angle = i * Mathf.PI * 2 / count + variant.rotationOffset * Mathf.Deg2Rad + Random.Range(-.33f, .33f);
                body.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                SpriteRenderer shadow = null;
                if (contactShadowSprite != null && shadowMaterial != null)
                {
                    var obj = new GameObject("Fragment contact shadow", typeof(SpriteRenderer));
                    obj.transform.SetParent(piece.transform, false);
                    obj.transform.localPosition = new Vector3(0, 0, .006f);
                    shadow = obj.GetComponent<SpriteRenderer>();
                    shadow.sprite = contactShadowSprite;
                    shadow.sharedMaterial = shadowMaterial;
                    shadow.sortingOrder = RemainsVisualStyle.DeadOrder;
                }

                var motion = piece.GetComponent<GibMotion2D>();
                motion.ConfigureVisual(renderer, shadow);
                // Чередуем тяжёлые близкие куски и быстрые дальние, а не восемь одинаковых импульсов.
                float power = Random.Range(profile.impulseRange.x, profile.impulseRange.y) * variant.impulseMultiplier * (i % 3 == 0 ? .65f : 1);
                float lift = Random.Range(profile.liftRange.x, profile.liftRange.y);
                if (specification != null)
                {
                    power *= specification.impulseMultiplier;
                    lift *= specification.liftMultiplier;
                }

                motion.Launch(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * power, Random.Range(-650f, 650f), obstacles.value | (1 << 9), lift, profile.gravity);
                piece.AddComponent<RemainsPartIdentity2D>().Configure(spriteIndex, renderer, shadow);
                record.RegisterPart(motion);
                FragmentLaunched?.Invoke(motion);
            }

            spawning = false;
        }

        private void CreateRemainsRoot(Vector3 position)
        {
            remainsRoot = new GameObject("Persistent remains — " + name).transform;
            remainsRoot.position = position;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(remainsRoot.gameObject, gameObject.scene);
            record = remainsRoot.gameObject.AddComponent<RemainsRecord2D>();
            record.Configure(bloodPoolPrefab, fragments, litMaterial, contactShadowSprite, shadowMaterial, variant != null ? variant.id : "restored");
        }

        public void ValidateRemains(RemainsSnapshot saved)
        {
            if (saved == null || profile == null || profile.variants == null ||
                System.Array.Find(profile.variants, item => item != null && item.id == saved.variantId) == null)
            {
                throw new System.InvalidOperationException("Missing saved gore variant");
            }

            if (saved.hasPool && bloodPoolPrefab == null)
            {
                throw new System.InvalidOperationException("Blood prefab layout changed");
            }

            if (saved.hasPool && !bloodPoolPrefab.SupportsVariant(saved.bloodVariantId))
            {
                throw new System.InvalidOperationException("Missing saved blood variant");
            }

            int poolRendererCount = saved.hasPool ? bloodPoolPrefab.GetComponentsInChildren<SpriteRenderer>(true).Length : 0;
            int fragmentCount = fragments != null ? fragments.Length : 0;
            RemainsSnapshotValidation.Validate(saved, fragmentCount, poolRendererCount);
        }

        public RemainsSnapshot CaptureRemains()
        {
            return record != null ? record.Capture() : null;
        }

        public void RestoreRemains(RemainsSnapshot saved)
        {
            if (saved != null)
            {
                ValidateRemains(saved);
            }

            StopAllCoroutines();
            spawning = false;
            emitted = true;
            if (remainsRoot != null)
            {
                Destroy(remainsRoot.gameObject);
            }

            if (saved == null)
            {
                return;
            }

            CreateRemainsRoot(saved.position);
            record.Restore(saved);
            GoreVariant restoredVariant = System.Array.Find(profile.variants, item => item != null && item.id == saved.variantId);
            if (restoredVariant != null && restoredVariant.replaceOriginalBody && replacedBody != null)
            {
                replacedBody.enabled = false;
            }
        }
    }
}
