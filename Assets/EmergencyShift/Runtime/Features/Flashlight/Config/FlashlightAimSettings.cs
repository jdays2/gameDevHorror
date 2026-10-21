using System;

namespace EmergencyShift.Features.Flashlight.Config
{
    public sealed class FlashlightAimSettings
    {
        public FlashlightAimSettings(float smoothTime, float maximumSpeed, float deadZone)
        {
            if (!float.IsFinite(smoothTime) || !float.IsFinite(maximumSpeed) || !float.IsFinite(deadZone) ||
                smoothTime <= 0 || maximumSpeed <= 0 || deadZone < 0)
            {
                throw new ArgumentException("Finite positive aim response and a nonnegative dead zone required.");
            }

            SmoothTime = smoothTime;
            MaximumSpeed = maximumSpeed;
            DeadZone = deadZone;
        }

        public float SmoothTime { get; }
        public float MaximumSpeed { get; }
        public float DeadZone { get; }
    }
}
