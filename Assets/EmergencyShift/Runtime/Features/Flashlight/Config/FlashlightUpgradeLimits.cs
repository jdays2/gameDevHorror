using System;

namespace EmergencyShift.Features.Flashlight.Config
{
    public readonly struct FlashlightUpgradeLimits
    {
        public FlashlightUpgradeLimits(float range, float angle)
        {
            if (!float.IsFinite(range) || !float.IsFinite(angle) || range < .5f || angle < 20 || angle > 140)
            {
                throw new ArgumentException("Flashlight limits require finite range >= .5 and angle 20..140.");
            }

            Range = range;
            Angle = angle;
        }

        public float Range { get; }
        public float Angle { get; }
        public bool IsValid => float.IsFinite(Range) && float.IsFinite(Angle) && Range >= .5f && Angle >= 20 && Angle <= 140;
    }
}
