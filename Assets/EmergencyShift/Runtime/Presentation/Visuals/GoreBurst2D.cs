using System;
using System.Collections;
using EmergencyShift.Core.Randomness;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Одноразовый разрыв тела. Лужа и куски независимы от игрового поведения врага.</summary>
    public sealed class GoreBurst2D : MonoBehaviour
    {
        private GoreSpawnBudget2D spawnBudget;
        private IRandomSource random;

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
        private void Construct(GoreSpawnBudget2D budget, [Inject(Id = RandomSourceIds.Cosmetic)] RandomSourceFactory randomSources)
        {
            spawnBudget = budget;
            random = randomSources.Create();
        }

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

            if (profile == null || profile.VariantCount == 0)
            {
                Debug.LogError("Missing gore profile: " + name, this);
                return;
            }

            variant = profile.ChooseVariant(random);
            for (int partIndex = 0; partIndex < variant.PieceCount; partIndex++)
            {
                GorePiece part = variant.PieceAt(partIndex);
                if (part == null || fragments == null || part.SpriteIndex < 0 || part.SpriteIndex >= fragments.Length || fragments[part.SpriteIndex] == null)
                {
                    Debug.LogError("Invalid anatomical gore composition: " + name, this);
                    return;
                }
            }

            emitted = true;
            CreateRemainsRoot(transform.position);
            spawning = true;
            // Кровь появляется сразу при переходе в Dead; разлёт частей следует за разрывом.
            if (bloodPoolPrefab != null)
            {
                BloodPool2D pool = Instantiate(bloodPoolPrefab, new Vector3(transform.position.x, transform.position.y, -0.02f), Quaternion.identity);
                pool.name = "Blood pool";
                pool.transform.SetParent(remainsRoot, true);
                record.RegisterPool(pool);
                pool.Initialize(profile.PoolSize * Range(0.85f, 1.18f), Range(1, int.MaxValue), obstacles);
                pool.BeginRupture();
                if (bloodSpraySprite != null)
                {
                    var spray = new GameObject("Blood rupture burst");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spray, gameObject.scene);
                    spray.transform.position = new Vector3(transform.position.x, transform.position.y, -.034f);
                    spray.AddComponent<BloodRuptureBurst2D>().Initialize(pool, bloodSpraySprite, litMaterial);
                }
            }

            StartCoroutine(Burst(obstacles));
        }

        private IEnumerator Burst(LayerMask obstacles)
        {
            // Первые кадры показывают разрыв, затем отсоединённые части получают своё движение.
            yield return new WaitForSeconds(profile.ReleaseDelay);
            if (fragments == null || fragments.Length == 0)
            {
                spawning = false;
                yield break;
            }

            bool explicitComposition = variant.PieceCount > 0;
            int count = explicitComposition ? variant.PieceCount : variant.FragmentCount;
            if (variant.ReplaceOriginalBody && replacedBody != null)
            {
                replacedBody.enabled = false;
            }

            for (int i = 0; i < count; i++)
            {
                while (!spawnBudget.TryReserve())
                {
                    yield return null;
                }

                GorePiece specification = explicitComposition ? variant.PieceAt(i) : null;
                int spriteIndex = specification != null ? specification.SpriteIndex : (i + variant.SpriteOffset) % fragments.Length;
                Vector2 scaleRange = specification != null ? specification.Scale : variant.Scale;
                var piece = new GameObject("Creature fragment", typeof(GibMotion2D));
                piece.transform.position = new Vector3(transform.position.x, transform.position.y, RemainsVisualStyle.SurfaceZ);
                if (specification != null)
                {
                    piece.transform.position += transform.TransformVector(specification.Offset);
                }
                piece.transform.SetParent(remainsRoot, true);
                var body = new GameObject("Fragment visual", typeof(SpriteRenderer));
                body.transform.SetParent(piece.transform, false);
                body.transform.localScale = Vector3.one * Range(scaleRange.x, scaleRange.y);
                var renderer = body.GetComponent<SpriteRenderer>();
                renderer.sprite = fragments[spriteIndex];
                renderer.sharedMaterial = litMaterial;
                renderer.sortingOrder = RemainsVisualStyle.DeadOrder;
                renderer.color = RemainsVisualStyle.Tint;
                float angle = i * Mathf.PI * 2 / count + variant.RotationOffset * Mathf.Deg2Rad + Range(-.33f, .33f);
                body.transform.rotation = Quaternion.Euler(0, 0, Range(0f, 360f));
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
                float power = Range(profile.ImpulseRange.x, profile.ImpulseRange.y) * variant.ImpulseMultiplier * (i % 3 == 0 ? .65f : 1);
                float lift = Range(profile.LiftRange.x, profile.LiftRange.y);
                if (specification != null)
                {
                    power *= specification.ImpulseMultiplier;
                    lift *= specification.LiftMultiplier;
                }

                motion.Launch(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * power, Range(-650f, 650f), obstacles.value | (1 << 9), lift, profile.Gravity);
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
            record.Configure(bloodPoolPrefab, fragments, litMaterial, contactShadowSprite, shadowMaterial, variant != null ? variant.Id : "restored");
        }

        public void ValidateRemains(RemainsSnapshot saved)
        {
            if (saved == null || profile == null || profile.FindVariant(saved.variantId) == null)
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
            GoreVariant restoredVariant = profile.FindVariant(saved.variantId);
            if (restoredVariant != null && restoredVariant.ReplaceOriginalBody && replacedBody != null)
            {
                replacedBody.enabled = false;
            }
        }

        private float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)random.NextUnit());
        private int Range(int minimum, int maximum) => random.NextInt(minimum, maximum);
    }
}
