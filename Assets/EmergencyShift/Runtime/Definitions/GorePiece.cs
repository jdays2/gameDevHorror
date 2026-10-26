using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class GorePiece
    {
        [SerializeField, Min(0)] private int spriteIndex;
        [SerializeField] private Vector2 offset;
        [SerializeField] private Vector2 scale = Vector2.one;
        [SerializeField, Min(.01f)] private float impulseMultiplier = 1;
        [SerializeField, Min(.01f)] private float liftMultiplier = 1;
        [SerializeField] private bool eyeSurface;
        [SerializeField, Tooltip("Complete carcass keeps the actor visual's facing and does not spin like a small gib.")]
        private bool preserveSourcePose;

        public int SpriteIndex => spriteIndex;
        public Vector2 Offset => offset;
        public Vector2 Scale => scale;
        public float ImpulseMultiplier => impulseMultiplier;
        public float LiftMultiplier => liftMultiplier;
        public bool EyeSurface => eyeSurface;
        public bool PreserveSourcePose => preserveSourcePose;

        public void Validate()
        {
            if (spriteIndex < 0 || !float.IsFinite(offset.x) || !float.IsFinite(offset.y) ||
                !float.IsFinite(scale.x) || !float.IsFinite(scale.y) || scale.x <= 0 || scale.y < scale.x ||
                !float.IsFinite(impulseMultiplier) || impulseMultiplier <= 0 ||
                !float.IsFinite(liftMultiplier) || liftMultiplier <= 0)
            {
                throw new InvalidOperationException("Invalid gore piece.");
            }
        }
    }
}
