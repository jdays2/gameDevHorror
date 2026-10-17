using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Stalker locomotion profile")]
    public sealed class StalkerLocomotionProfile : ScriptableObject
    {
        [Tooltip("Travel per complete eight-frame gait cycle in local creature units. Spawn scale is applied at runtime.")]
        [Min(0.05f)]
        public float distancePerCycle = 1.05f;

        [Tooltip("Longer support phases at the start and middle of the gait; zero gives uniform frame timing.")]
        [Range(0, 0.6f)]
        public float supportHold = 0.35f;

        [Tooltip("Small alternating turn of the whole visual around its center. No vertical bounce or scaling.")]
        [Range(0, 3)]
        public float supportYawDegrees = 0.9f;

        [Min(0.01f)]
        public float minimumMovingSpeed = 0.025f;

        [Min(0.1f)]
        public float fullWeightSpeed = 1.1f;

        [Min(0.01f)]
        public float startSeconds = 0.12f;

        [Min(0.01f)]
        public float settleSeconds = 0.1f;

        [Tooltip("Directional response; the animator's existing turnSpeed remains the angular speed limit.")]
        [Min(0.01f)]
        public float turnSmoothSeconds = 0.1f;
    }
}
