using System;
using UnityEngine;
using EmergencyShift.Core.Randomness;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Gore profile")]
    public sealed class GoreProfile : ScriptableObject
    {
        [Min(.1f)]
        [SerializeField]
        private float poolSize = 1.35f;
        [Min(0)]
        [SerializeField]
        private float releaseDelay = .12f;
        [SerializeField]
        private Vector2 impulseRange = new Vector2(3.2f, 9.5f);
        [SerializeField]
        private Vector2 liftRange = new Vector2(3, 5.5f);
        [Min(1)]
        [SerializeField]
        private float gravity = 26;
        [SerializeField, Tooltip("Tint of new settled fragments; keep RGB below one so remains stay darker than living actors.")]
        private Color settledTint = new Color(.78f, .73f, .76f, 1);
        [SerializeField]
        private Color airborneTint = new Color(.88f, .82f, .86f, 1);
        [SerializeField]
        private GoreVariant[] variants =
        {
            new GoreVariant("radial")
        };

        public float PoolSize => poolSize;
        public float ReleaseDelay => releaseDelay;
        public Vector2 ImpulseRange => impulseRange;
        public Vector2 LiftRange => liftRange;
        public float Gravity => gravity;
        public Color SettledTint => settledTint;
        public Color AirborneTint => airborneTint;
        public int VariantCount => variants != null ? variants.Length : 0;
        public GoreVariant VariantAt(int index) => variants[index];

        public GoreVariant ChooseVariant(IRandomSource random)
        {
            Validate();
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int count = 0;
            foreach (GoreVariant variant in variants)
            {
                if (variant != null && !variant.RestorationOnly)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                throw new System.InvalidOperationException("Gore profile has no composition for new deaths: " + name);
            }

            int selected = random.NextInt(0, count);
            if (selected < 0 || selected >= count)
            {
                throw new InvalidOperationException("Invalid gore variant random index.");
            }

            foreach (GoreVariant variant in variants)
            {
                if (variant == null || variant.RestorationOnly)
                {
                    continue;
                }

                if (selected == 0)
                {
                    return variant;
                }
                selected--;
            }

            throw new System.InvalidOperationException("Gore composition selection failed");
        }

        public GoreVariant FindVariant(string id)
        {
            Validate();
            for (int index = 0; index < VariantCount; index++)
            {
                if (string.Equals(variants[index].Id, id, StringComparison.Ordinal))
                {
                    return variants[index];
                }
            }

            return null;
        }

        public void Validate()
        {
            if (!float.IsFinite(poolSize) || poolSize < .1f || !float.IsFinite(releaseDelay) || releaseDelay < 0 ||
                !float.IsFinite(gravity) || gravity < 1 || VariantCount == 0 ||
                !ValidRange(impulseRange) || !ValidRange(liftRange) || !ValidTint(settledTint) || !ValidTint(airborneTint))
            {
                throw new InvalidOperationException("Invalid gore profile: " + name);
            }

            for (int index = 0; index < VariantCount; index++)
            {
                if (variants[index] == null)
                {
                    throw new InvalidOperationException("Missing gore variant.");
                }

                variants[index].Validate();
                for (int prior = 0; prior < index; prior++)
                {
                    if (string.Equals(variants[prior].Id, variants[index].Id, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Duplicate gore variant ID.");
                    }
                }
            }
        }

        private static bool ValidRange(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && value.x >= 0 && value.y >= value.x;

        private static bool ValidTint(Color value) => float.IsFinite(value.r) && float.IsFinite(value.g) && float.IsFinite(value.b)
            && value.r > 0 && value.r <= 1 && value.g > 0 && value.g <= 1 && value.b > 0 && value.b <= 1 && value.a == 1;
    }
}
