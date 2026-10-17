using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class GorePiece
    {
        [Min(0)]
        public int spriteIndex;
        public Vector2 offset;
        public Vector2 scale = Vector2.one;
        [Min(.01f)]
        public float impulseMultiplier = 1;
        [Min(.01f)]
        public float liftMultiplier = 1;
    }

    [Serializable]
    public sealed class GoreVariant
    {
        public string id;
        [Min(1)]
        public int fragmentCount = 8;
        public int spriteOffset;
        public float rotationOffset;
        public Vector2 scale = new Vector2(.65f, .95f);
        [Min(.1f)]
        public float impulseMultiplier = 1;
        [Tooltip("Optional explicit anatomical composition. Empty preserves the original radial fragment layout.")]
        public GorePiece[] pieces;
        public bool replaceOriginalBody;
        [Tooltip("Keep the ID available for old snapshots, but do not select this composition for a new death.")]
        public bool restorationOnly;
    }

    [CreateAssetMenu(menuName = "Emergency Shift/Gore profile")]
    public sealed class GoreProfile : ScriptableObject
    {
        [Min(.1f)]
        public float poolSize = 1.35f;
        [Min(0)]
        public float releaseDelay = .12f;
        public Vector2 impulseRange = new Vector2(3.2f, 9.5f);
        public Vector2 liftRange = new Vector2(3, 5.5f);
        [Min(1)]
        public float gravity = 26;
        public GoreVariant[] variants =
        {
            new GoreVariant
            {
                id = "radial"
            }
        };

        public GoreVariant ChooseVariant()
        {
            int count = 0;
            foreach (GoreVariant variant in variants)
            {
                if (variant != null && !variant.restorationOnly)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                throw new System.InvalidOperationException("Gore profile has no composition for new deaths: " + name);
            }

            int selected = UnityEngine.Random.Range(0, count);
            foreach (GoreVariant variant in variants)
            {
                if (variant == null || variant.restorationOnly)
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
    }
}
