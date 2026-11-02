using System;

namespace EmergencyShift.Features.WorldAudio.Config
{
    public sealed class WorldAudioSettings
    {
        public float FadeOutSeconds { get; }
        public float RestoreSeconds { get; }
        public WorldAudioFadeCurve Curve { get; }

        public WorldAudioSettings(float fadeOutSeconds, float restoreSeconds, WorldAudioFadeCurve curve)
        {
            if (!float.IsFinite(fadeOutSeconds) || fadeOutSeconds < .05f ||
                !float.IsFinite(restoreSeconds) || restoreSeconds < .05f ||
                curve < WorldAudioFadeCurve.Linear || curve > WorldAudioFadeCurve.SmootherStep)
            {
                throw new ArgumentException("World audio needs finite fades of at least 0.05 seconds and a supported curve.");
            }

            FadeOutSeconds = fadeOutSeconds;
            RestoreSeconds = restoreSeconds;
            Curve = curve;
        }

        public float Evaluate(float progress)
        {
            switch (Curve)
            {
                case WorldAudioFadeCurve.SmoothStep:
                    return progress * progress * (3 - 2 * progress);
                case WorldAudioFadeCurve.SmootherStep:
                    return progress * progress * progress * (progress * (6 * progress - 15) + 10);
                default:
                    return progress;
            }
        }
    }
}
