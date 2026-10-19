using System;

namespace EmergencyShift.Features.DamageFeedback.Config
{
    public readonly struct DamageFeedbackSettings
    {
        public float HoldSeconds { get; }
        public float FadeSeconds { get; }
        public float Opacity { get; }

        public DamageFeedbackSettings(float holdSeconds, float fadeSeconds, float opacity)
        {
            if (float.IsNaN(holdSeconds) || float.IsInfinity(holdSeconds) || holdSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(holdSeconds));
            }

            if (float.IsNaN(fadeSeconds) || float.IsInfinity(fadeSeconds) || fadeSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeSeconds));
            }

            if (float.IsNaN(opacity) || float.IsInfinity(opacity) || opacity <= 0 || opacity > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(opacity));
            }

            HoldSeconds = holdSeconds;
            FadeSeconds = fadeSeconds;
            Opacity = opacity;
        }
    }
}
