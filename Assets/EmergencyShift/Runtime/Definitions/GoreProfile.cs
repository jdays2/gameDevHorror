using System;
using UnityEngine;

namespace EmergencyShift
{
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
    }
}
