using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Stalker locomotion profile")]
    public sealed class StalkerLocomotionProfile : ScriptableObject
    {
        [Tooltip("Travel per complete eight-frame gait cycle in local creature units. Spawn scale is applied at runtime.")]
        [Min(0.05f)]
        [SerializeField]
        private float distancePerCycle = 1.05f;

        [Tooltip("Longer support phases at the start and middle of the gait; zero gives uniform frame timing.")]
        [Range(0, 0.6f)]
        [SerializeField]
        private float supportHold = 0.35f;

        [Tooltip("Small alternating turn of the whole visual around its center. No vertical bounce or scaling.")]
        [Range(0, 3)]
        [SerializeField]
        private float supportYawDegrees = 0.9f;

        [Min(0.01f)]
        [SerializeField]
        private float minimumMovingSpeed = 0.025f;

        [Min(0.1f)]
        [SerializeField]
        private float fullWeightSpeed = 1.1f;

        [Min(0.01f)]
        [SerializeField]
        private float startSeconds = 0.12f;

        [Min(0.01f)]
        [SerializeField]
        private float settleSeconds = 0.1f;

        [Tooltip("Directional response; the animator's existing turnSpeed remains the angular speed limit.")]
        [Min(0.01f)]
        [SerializeField]
        private float turnSmoothSeconds = 0.1f;

        public float DistancePerCycle => distancePerCycle;
        public float SupportHold => supportHold;
        public float SupportYawDegrees => supportYawDegrees;
        public float MinimumMovingSpeed => minimumMovingSpeed;
        public float FullWeightSpeed => fullWeightSpeed;
        public float StartSeconds => startSeconds;
        public float SettleSeconds => settleSeconds;
        public float TurnSmoothSeconds => turnSmoothSeconds;

        public void Validate()
        {
            if (!float.IsFinite(distancePerCycle) || distancePerCycle < .05f ||
                !float.IsFinite(supportHold) || supportHold < 0 || supportHold > .6f ||
                !float.IsFinite(supportYawDegrees) || supportYawDegrees < 0 || supportYawDegrees > 3 ||
                !float.IsFinite(minimumMovingSpeed) || minimumMovingSpeed < .01f ||
                !float.IsFinite(fullWeightSpeed) || fullWeightSpeed < .1f ||
                !float.IsFinite(startSeconds) || startSeconds < .01f ||
                !float.IsFinite(settleSeconds) || settleSeconds < .01f ||
                !float.IsFinite(turnSmoothSeconds) || turnSmoothSeconds < .01f)
            {
                throw new InvalidOperationException("Invalid Stalker locomotion profile: " + name);
            }
        }
    }
}
