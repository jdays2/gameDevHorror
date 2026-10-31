using System;
using System.Collections;
using System.Collections.Generic;
using EmergencyShift.Core.Randomness;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Scene-owned settled remains. It survives the actor; no AI, damage or playback logic.</summary>
    public sealed class RemainsRecord2D : MonoBehaviour
    {
        private BloodPool2D bloodPoolPrefab;
        private Sprite[] fragments;
        private Material litMaterial;
        private Sprite contactShadowSprite;
        private Material shadowMaterial;
        private Material eyeSurfaceMaterial;
        private float eyeDamage;
        private BloodPool2D spawnedPool;
        private readonly List<GibMotion2D> movingParts = new List<GibMotion2D>();
        private string variantId;
        private GoreProfile profile;
        private GoreVariant variant;
        private GoreSpawnBudget2D spawnBudget;
        private IRandomSource random;
        private Matrix4x4 sourcePose;
        private SpriteRenderer replacedBody;
        private Vector2[] poseOffsets;
        private int[] poseIndices;
        private bool emitting;
        private bool hasPresentation;
        private bool assembledSorting;
        public event Action<GibMotion2D> FragmentLaunched;
        public void Configure(BloodPool2D pool, Sprite[] sprites, Material material, Sprite shadow, Material shadowStyle, string variant,
            Material eyeMaterial = null, bool assemble = false)
        {
            bloodPoolPrefab = pool;
            fragments = sprites;
            litMaterial = material;
            contactShadowSprite = shadow;
            shadowMaterial = shadowStyle;
            variantId = variant;
            eyeSurfaceMaterial = eyeMaterial;
            assembledSorting = assemble;
        }

        public void RegisterPool(BloodPool2D pool)
        {
            spawnedPool = pool;
        }

        public void RegisterPart(GibMotion2D part)
        {
            movingParts.Add(part);
        }

        public void BeginEmission(GoreProfile definition, GoreVariant composition, GoreSpawnBudget2D budget,
            IRandomSource randomSource, LayerMask obstacles, Matrix4x4 sourceTransform, SpriteRenderer originalBody, float ocularDamage = 0,
            Vector2[] partOffsets = null, int[] partIndices = null)
        {
            if (hasPresentation || definition == null || composition == null || randomSource == null ||
                (fragments != null && fragments.Length > 0 && budget == null))
            {
                throw new InvalidOperationException("Remains emission requires a fresh configured view and explicit dependencies.");
            }

            profile = definition;
            variant = composition;
            spawnBudget = budget;
            random = randomSource;
            sourcePose = sourceTransform;
            replacedBody = originalBody;
            eyeDamage = ocularDamage;
            poseOffsets = partOffsets;
            poseIndices = partIndices;
            hasPresentation = true;
            emitting = true;
            StartCoroutine(Burst(obstacles));
        }

        private IEnumerator Burst(LayerMask obstacles)
        {
            try
            {
                // Первые кадры показывают разрыв, затем отсоединённые части получают своё движение.
                yield return new WaitForSeconds(profile.ReleaseDelay);
                if (fragments == null || fragments.Length == 0)
                {
                    yield break;
                }

                bool explicitComposition = variant.PieceCount > 0;
                int count = explicitComposition ? variant.PieceCount : variant.FragmentCount;
                if (variant.AssembleBeforeRelease)
                {
                    while (!spawnBudget.TryReserve(count))
                    {
                        yield return null;
                    }
                }
                for (int i = 0; i < count; i++)
                {
                    while (!variant.AssembleBeforeRelease && !spawnBudget.TryReserve())
                    {
                        yield return null;
                    }

                    GorePiece specification = explicitComposition ? variant.PieceAt(i) : null;
                    int spriteIndex = specification != null ? specification.SpriteIndex : (i + variant.SpriteOffset) % fragments.Length;
                    if (poseIndices != null)
                    {
                        spriteIndex = poseIndices[spriteIndex];
                    }
                    Vector2 scaleRange = specification != null ? specification.Scale : variant.Scale;
                    var piece = new GameObject("Creature fragment", typeof(GibMotion2D));
                    piece.transform.position = new Vector3(transform.position.x, transform.position.y, RemainsVisualStyle.SurfaceZ);
                    if (specification != null)
                    {
                        Vector2 offset = poseOffsets != null && spriteIndex < poseOffsets.Length ? poseOffsets[spriteIndex] : specification.Offset;
                        piece.transform.position += sourcePose.MultiplyVector(offset);
                    }
                    piece.transform.SetParent(transform, true);
                    var body = new GameObject("Fragment visual", typeof(SpriteRenderer));
                    body.transform.SetParent(piece.transform, false);
                    body.transform.localScale = Vector3.one * Range(scaleRange.x, scaleRange.y);
                    var renderer = body.GetComponent<SpriteRenderer>();
                    renderer.sprite = fragments[spriteIndex];
                    renderer.sharedMaterial = litMaterial;
                    renderer.sortingOrder = variant.AssembleBeforeRelease ? i + 1 : RemainsVisualStyle.DeadOrder;
                    renderer.color = profile.SettledTint;
                    // A busy spawn budget must not leave a gap before the first replacement exists.
                    if (i == 0 && variant.ReplaceOriginalBody && replacedBody != null)
                    {
                        replacedBody.enabled = false;
                    }
                    bool hasEyeSurface = specification != null && specification.EyeSurface;
                    if (hasEyeSurface)
                    {
                        RemainsEyeSurface.Apply(renderer, eyeSurfaceMaterial, eyeDamage);
                    }
                    float angle = i * Mathf.PI * 2 / count + variant.RotationOffset * Mathf.Deg2Rad + Range(-.33f, .33f);
                    bool preservePose = specification != null && specification.PreserveSourcePose;
                    body.transform.rotation = (preservePose || specification != null && specification.AlignSourcePose) && replacedBody != null
                        ? replacedBody.transform.rotation : Quaternion.Euler(0, 0, Range(0f, 360f));
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
                    motion.ConfigureTint(profile.SettledTint, profile.AirborneTint);
                    // Чередуем тяжёлые близкие куски и быстрые дальние, а не восемь одинаковых импульсов.
                    float power = Range(profile.ImpulseRange.x, profile.ImpulseRange.y) * variant.ImpulseMultiplier * (i % 3 == 0 ? .65f : 1);
                    float lift = Range(profile.LiftRange.x, profile.LiftRange.y);
                    if (specification != null)
                    {
                        power *= specification.ImpulseMultiplier;
                        lift *= specification.LiftMultiplier;
                    }

                    float angularSpeed = preservePose ? 0 : Range(-650f, 650f);
                    motion.Launch(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * power, angularSpeed, obstacles.value | (1 << 9), lift, profile.Gravity);
                    piece.AddComponent<RemainsPartIdentity2D>().Configure(spriteIndex, renderer, shadow, hasEyeSurface,
                        hasEyeSurface ? eyeDamage : 0);
                    RegisterPart(motion);
                    FragmentLaunched?.Invoke(motion);
                }

            }
            finally
            {
                emitting = false;
            }
        }

        private float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)random.NextUnit());

        private void OnDisable()
        {
            StopAllCoroutines();
            emitting = false;
        }

        public bool IsSettled
        {
            get
            {
                if (emitting)
                {
                    return false;
                }

                if (spawnedPool != null && spawnedPool.enabled)
                {
                    return false;
                }

                foreach (var part in movingParts)
                {
                    if (part != null && !part.Settled)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public RemainsSnapshot Capture()
        {
            if (transform == null)
            {
                return null;
            }

            var result = new RemainsSnapshot
            {
                version = RemainsSnapshot.CurrentVersion,
                variantId = variantId,
                position = transform.position
            };
            if (spawnedPool != null)
            {
                var renderers = spawnedPool.GetComponentsInChildren<SpriteRenderer>(true);
                result.hasPool = true;
                result.bloodVariantId = spawnedPool.VariantId;
                result.pool = new RemainsSpriteState[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    result.pool[i] = new RemainsSpriteState();
                    result.pool[i].Capture(renderers[i]);
                }
            }

            var visuals = GetComponentsInChildren<RemainsPartIdentity2D>(true);
            result.parts = new RemainsPartState[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                result.parts[i] = visuals[i].Capture();
            }

            return result;
        }

        public void Restore(RemainsSnapshot saved)
        {
            if (saved == null)
            {
                return;
            }

            if (saved.hasPool && (bloodPoolPrefab == null || !bloodPoolPrefab.SupportsVariant(saved.bloodVariantId)))
            {
                throw new System.InvalidOperationException("Missing saved blood variant");
            }

            int poolRendererCount = saved.hasPool ? bloodPoolPrefab.GetComponentsInChildren<SpriteRenderer>(true).Length : 0;
            int fragmentCount = fragments != null ? fragments.Length : 0;
            RemainsSnapshotValidation.Validate(saved, fragmentCount, poolRendererCount);

            foreach (RemainsPartState part in saved.parts)
            {
                if (part.hasEyeSurface && eyeSurfaceMaterial == null)
                {
                    throw new InvalidOperationException("Saved ocular surface requires its configured material.");
                }
            }

            if (hasPresentation)
            {
                throw new InvalidOperationException("Restore requires a fresh remains view.");
            }

            hasPresentation = true;
            variantId = saved.variantId;
            transform.position = saved.position;
            if (saved.hasPool && bloodPoolPrefab != null)
            {
                spawnedPool = Instantiate(bloodPoolPrefab, new Vector3(saved.position.x, saved.position.y, -.02f), Quaternion.identity, transform);
                spawnedPool.enabled = false;
                spawnedPool.RestoreVariant(saved.bloodVariantId);
                var renderers = spawnedPool.GetComponentsInChildren<SpriteRenderer>(true);
                if (renderers.Length != saved.pool.Length)
                {
                    throw new System.InvalidOperationException("Blood prefab layout differs from checkpoint");
                }

                for (int i = 0; i < renderers.Length; i++)
                {
                    saved.pool[i].Apply(renderers[i]);
                }
            }

            if (saved.parts == null)
            {
                return;
            }

            for (int partIndex = 0; partIndex < saved.parts.Length; partIndex++)
            {
                var part = saved.parts[partIndex];
                if (part.spriteIndex < 0 || part.spriteIndex >= fragments.Length)
                {
                    throw new System.InvalidOperationException("Missing saved fragment");
                }

                var root = new GameObject("Settled fragment", typeof(RemainsPartIdentity2D));
                root.transform.SetParent(transform, false);
                root.transform.position = part.position;
                var body = new GameObject("Fragment visual", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                body.transform.SetParent(root.transform, false);
                body.sprite = fragments[part.spriteIndex];
                body.sharedMaterial = litMaterial;
                body.sortingOrder = assembledSorting ? partIndex + 1 : RemainsVisualStyle.DeadOrder;
                part.body.Apply(body);
                if (part.hasEyeSurface)
                {
                    RemainsEyeSurface.Apply(body, eyeSurfaceMaterial, part.eyeDamage);
                }
                SpriteRenderer shadow = null;
                if (part.hasShadow)
                {
                    shadow = new GameObject("Fragment contact shadow", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                    shadow.transform.SetParent(root.transform, false);
                    shadow.sprite = contactShadowSprite;
                    shadow.sharedMaterial = shadowMaterial;
                    shadow.sortingOrder = RemainsVisualStyle.DeadOrder;
                    part.shadow.Apply(shadow);
                }

                root.GetComponent<RemainsPartIdentity2D>().Configure(part.spriteIndex, body, shadow, part.hasEyeSurface, part.eyeDamage);
            }
        }
    }
}
