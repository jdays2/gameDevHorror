using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class BloodPoolVariant
    {
        public string id;
        public Sprite sprite;
        [Min(.1f)]
        public float canvasSize = 4.5f;
    }

    /// <summary>Shared baked shapes and presentation tuning; each death owns its choice and transforms.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Blood pool profile")]
    public sealed class BloodPoolProfile : ScriptableObject
    {
        public BloodPoolVariant[] variants;
        public Sprite[] dropletSprites;
        public Color poolTint = Color.white;
        public Color dropletTint = Color.white;
        public Vector2 scaleRange = new Vector2(.92f, 1.08f);
        public Vector2 dropletDistance = new Vector2(.7f, 1.8f);
        public Vector2 dropletSize = new Vector2(.06f, .18f);
        public Vector2 dropletAspect = new Vector2(.6f, 1.5f);
        public Vector2 arrivalSeconds = new Vector2(.18f, .34f);
        [Min(.01f)]
        public float growSeconds = .22f;
        [Range(.01f, 1)]
        public float initialScale = .3f;

        public int FindVariant(string id)
        {
            return Array.FindIndex(variants, variant => variant != null && variant.id == id);
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
                if (variant == null || string.IsNullOrEmpty(variant.id) || variant.sprite == null
                    || !float.IsFinite(variant.canvasSize) || variant.canvasSize <= 0)
                {
                    return false;
                }

                if (Array.FindIndex(variants, item => item != null && item.id == variant.id) != i)
                {
                    return false;
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

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);

        private static bool Finite(Color value) => float.IsFinite(value.r) && float.IsFinite(value.g)
            && float.IsFinite(value.b) && float.IsFinite(value.a);
    }
}
