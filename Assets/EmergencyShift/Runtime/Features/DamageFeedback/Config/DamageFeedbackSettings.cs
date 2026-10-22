using System;

namespace EmergencyShift.Features.DamageFeedback.Config
{
    public readonly struct DamageFeedbackSettings
    {
        public float HoldSeconds { get; }
        public float FadeSeconds { get; }
        public float Opacity { get; }
        public float CameraDistance { get; }
        public float CameraSeconds { get; }

        public DamageFeedbackSettings(float holdSeconds, float fadeSeconds, float opacity,
            float cameraDistance = 0, float cameraSeconds = .22f)
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

            if (!float.IsFinite(cameraDistance) || cameraDistance < 0 || cameraDistance > .25f)
            {
                throw new ArgumentOutOfRangeException(nameof(cameraDistance));
            }

            if (!float.IsFinite(cameraSeconds) || cameraSeconds <= 0 || cameraSeconds > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(cameraSeconds));
            }

            HoldSeconds = holdSeconds;
            FadeSeconds = fadeSeconds;
            Opacity = opacity;
            CameraDistance = cameraDistance;
            CameraSeconds = cameraSeconds;
        }
    }
}
