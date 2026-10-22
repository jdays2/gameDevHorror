using System;
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
        private RemainsRecord2D subscribedRecord;
        private bool emitted;

        public bool IsSettled => record == null || record.IsSettled;
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

            if (fragments != null && fragments.Length > 0 && spawnBudget == null)
            {
                throw new InvalidOperationException("Gore emission requires its world spawn budget.");
            }

            emitted = true;
            CreateRemainsRoot(transform.position);
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

            record.BeginEmission(profile, variant, spawnBudget, random, obstacles, transform.localToWorldMatrix, replacedBody);
        }

        private void CreateRemainsRoot(Vector3 position)
        {
            remainsRoot = new GameObject("Persistent remains — " + name).transform;
            remainsRoot.position = position;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(remainsRoot.gameObject, gameObject.scene);
            record = remainsRoot.gameObject.AddComponent<RemainsRecord2D>();
            record.Configure(bloodPoolPrefab, fragments, litMaterial, contactShadowSprite, shadowMaterial, variant != null ? variant.Id : "restored");
            BindRecord();
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

            DetachRecord();
            emitted = true;
            if (remainsRoot != null)
            {
                remainsRoot.gameObject.SetActive(false);
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

        private void OnEnable() => BindRecord();
        private void OnDisable() => DetachRecord();
        private void OnDestroy() => DetachRecord();

        private void BindRecord()
        {
            if (subscribedRecord != null || record == null || !isActiveAndEnabled)
            {
                return;
            }

            subscribedRecord = record;
            subscribedRecord.FragmentLaunched += OnFragment;
        }

        private void DetachRecord()
        {
            if (!ReferenceEquals(subscribedRecord, null))
            {
                subscribedRecord.FragmentLaunched -= OnFragment;
            }

            subscribedRecord = null;
        }

        private void OnFragment(GibMotion2D part) => FragmentLaunched?.Invoke(part);

        private float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)random.NextUnit());
        private int Range(int minimum, int maximum) => random.NextInt(minimum, maximum);
    }
}
