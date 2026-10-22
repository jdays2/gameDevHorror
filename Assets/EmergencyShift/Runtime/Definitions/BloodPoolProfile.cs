using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared baked shapes and presentation tuning; each death owns its choice and transforms.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Blood pool profile")]
    public sealed class BloodPoolProfile : ScriptableObject
    {
        [SerializeField]
        private BloodPoolVariant[] variants;
        [SerializeField]
        private Sprite[] dropletSprites;
        [SerializeField]
        private Color poolTint = Color.white;
        [SerializeField]
        private Color dropletTint = Color.white;
        [SerializeField]
        private Vector2 scaleRange = new Vector2(.92f, 1.08f);
        [SerializeField]
        private Vector2 dropletDistance = new Vector2(.7f, 1.8f);
        [SerializeField]
        private Vector2 dropletSize = new Vector2(.06f, .18f);
        [SerializeField]
        private Vector2 dropletAspect = new Vector2(.6f, 1.5f);
        [SerializeField]
        private Vector2 arrivalSeconds = new Vector2(.18f, .34f);
        [Min(.01f)]
        [SerializeField]
        private float growSeconds = .22f;
        [Range(.01f, 1)]
        [SerializeField]
        private float initialScale = .3f;

        public Color PoolTint => poolTint;
        public Color DropletTint => dropletTint;
        public Vector2 ScaleRange => scaleRange;
        public Vector2 DropletDistance => dropletDistance;
        public Vector2 DropletSize => dropletSize;
        public Vector2 DropletAspect => dropletAspect;
        public Vector2 ArrivalSeconds => arrivalSeconds;
        public float GrowSeconds => growSeconds;
        public float InitialScale => initialScale;
        public int VariantCount => variants != null ? variants.Length : 0;
        public BloodPoolVariant VariantAt(int index) => variants[index];
        public int DropletSpriteCount => dropletSprites != null ? dropletSprites.Length : 0;
        public Sprite DropletSpriteAt(int index) => dropletSprites[index];

        public int FindVariant(string id)
        {
            Validate();
            for (int index = 0; index < VariantCount; index++)
            {
                if (string.Equals(variants[index].Id, id, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        public bool IsValid()
        {
            if (variants == null || variants.Length == 0 || dropletSprites == null || dropletSprites.Length == 0
                || !Finite(poolTint) || !Finite(dropletTint) || !Finite(scaleRange) || !Finite(dropletDistance)
                || !Finite(dropletSize) || !Finite(dropletAspect) || !Finite(arrivalSeconds)
                || !float.IsFinite(growSeconds) || !float.IsFinite(initialScale))
            {
                return false;
            }

            for (int i = 0; i < variants.Length; i++)
            {
                BloodPoolVariant variant = variants[i];
                if (variant == null || !variant.IsValid())
                {
                    return false;
                }

                for (int prior = 0; prior < i; prior++)
                {
                    if (string.Equals(variants[prior].Id, variant.Id, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return Array.TrueForAll(dropletSprites, sprite => sprite != null)
                && scaleRange.x > 0 && scaleRange.y >= scaleRange.x
                && dropletDistance.x >= 0 && dropletDistance.y >= dropletDistance.x
                && dropletSize.x > 0 && dropletSize.y >= dropletSize.x
                && dropletAspect.x > 0 && dropletAspect.y >= dropletAspect.x
                && arrivalSeconds.x > 0 && arrivalSeconds.y >= arrivalSeconds.x
                && growSeconds > 0 && initialScale > 0 && initialScale <= 1;
        }

        public void Validate()
        {
            if (!IsValid())
            {
                throw new InvalidOperationException("Invalid blood pool profile: " + name);
            }
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);

        private static bool Finite(Color value) => float.IsFinite(value.r) && float.IsFinite(value.g)
            && float.IsFinite(value.b) && float.IsFinite(value.a);
    }
}
