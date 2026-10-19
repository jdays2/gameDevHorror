using System.Collections.Generic;
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
        private BloodPool2D spawnedPool;
        private readonly List<GibMotion2D> movingParts = new List<GibMotion2D>();
        private string variantId;
        public void Configure(BloodPool2D pool, Sprite[] sprites, Material material, Sprite shadow, Material shadowStyle, string variant)
        {
            bloodPoolPrefab = pool;
            fragments = sprites;
            litMaterial = material;
            contactShadowSprite = shadow;
            shadowMaterial = shadowStyle;
            variantId = variant;
        }

        public void RegisterPool(BloodPool2D pool)
        {
            spawnedPool = pool;
        }

        public void RegisterPart(GibMotion2D part)
        {
            movingParts.Add(part);
        }

        public bool IsSettled
        {
            get
            {
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

            foreach (var part in saved.parts)
            {
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
                body.sortingOrder = RemainsVisualStyle.DeadOrder;
                part.body.Apply(body);
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

                root.GetComponent<RemainsPartIdentity2D>().Configure(part.spriteIndex, body, shadow);
            }
        }
    }
}
