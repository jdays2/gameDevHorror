using System;

namespace EmergencyShift.Features.Pickups.View
{
    /// <summary>Cosmetic instance timing. A held beam does not loop; darkness rearms discovery.</summary>
    public sealed class PickupGlintPulse
    {
        private readonly float duration;
        private readonly float rearmSeconds;
        private float elapsed;
        private float darkSeconds;
        private bool wasVisible;

        public bool Active { get; private set; }
        public float Progress => elapsed / duration;

        public PickupGlintPulse(float duration, float rearmSeconds)
        {
            if (!Finite(duration) || duration <= 0 || !Finite(rearmSeconds) || rearmSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            this.duration = duration;
            this.rearmSeconds = rearmSeconds;
            Reset();
        }

        public void Step(bool visible, float deltaTime)
        {
            if (!Finite(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (!visible)
            {
                Active = false;
                wasVisible = false;
                darkSeconds = Math.Min(rearmSeconds, darkSeconds + deltaTime);
                return;
            }

            if (!wasVisible && darkSeconds >= rearmSeconds)
            {
                elapsed = 0;
                Active = true;
            }

            wasVisible = true;
            darkSeconds = 0;
            if (Active)
            {
                elapsed = Math.Min(duration, elapsed + deltaTime);
                Active = elapsed < duration;
            }
        }

        public void Reset()
        {
            elapsed = 0;
            darkSeconds = rearmSeconds;
            wasVisible = false;
            Active = false;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
