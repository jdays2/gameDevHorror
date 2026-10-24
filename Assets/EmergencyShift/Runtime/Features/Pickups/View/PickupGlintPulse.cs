using System;

namespace EmergencyShift.Features.Pickups.View
{
    /// <summary>Cosmetic instance timing. Every beam entry restarts the sweep; a held beam repeats after a pause.</summary>
    public sealed class PickupGlintPulse
    {
        private readonly double duration;
        private readonly double cycleDuration;
        private double elapsed;

        public bool Active { get; private set; }
        public float Progress => (float)(Math.Min(elapsed, duration) / duration);

        public PickupGlintPulse(float duration, float repeatPause)
        {
            if (!Finite(duration) || duration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            if (!Finite(repeatPause) || repeatPause < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatPause));
            }

            this.duration = duration;
            cycleDuration = this.duration + repeatPause;
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
                Reset();
                return;
            }

            elapsed = (elapsed + deltaTime) % cycleDuration;
            Active = elapsed < duration;
        }

        public void Reset()
        {
            elapsed = 0;
            Active = false;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
