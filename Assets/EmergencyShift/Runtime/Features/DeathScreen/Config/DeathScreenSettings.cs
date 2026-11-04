using System;

namespace EmergencyShift.Features.DeathScreen.Config
{
    public sealed class DeathScreenSettings
    {
        public float WorldFadeSeconds { get; }
        public float ContentDelaySeconds { get; }
        public float ContentFadeSeconds { get; }
        public float ExitFadeSeconds { get; }
        public float IrisFeather { get; }
        public float WorldPauseDelaySeconds { get; }
        public float ContentStartSeconds => Math.Max(WorldFadeSeconds, ContentDelaySeconds);
        public float EntrySeconds => ContentStartSeconds + ContentFadeSeconds;

        public DeathScreenSettings(float worldFadeSeconds, float contentDelaySeconds,
            float contentFadeSeconds, float exitFadeSeconds, float irisFeather = .18f)
            : this(worldFadeSeconds, contentDelaySeconds, contentFadeSeconds, exitFadeSeconds,
                irisFeather, worldFadeSeconds)
        {
        }

        public DeathScreenSettings(float worldFadeSeconds, float contentDelaySeconds,
            float contentFadeSeconds, float exitFadeSeconds, float irisFeather, float worldPauseDelaySeconds)
        {
            RequireDuration(worldFadeSeconds, nameof(worldFadeSeconds));
            RequireDuration(contentDelaySeconds, nameof(contentDelaySeconds));
            RequireDuration(contentFadeSeconds, nameof(contentFadeSeconds));
            RequireDuration(exitFadeSeconds, nameof(exitFadeSeconds));
            RequireDuration(worldPauseDelaySeconds, nameof(worldPauseDelaySeconds));
            if (!float.IsFinite(irisFeather) || irisFeather <= 0 || irisFeather > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(irisFeather), "The aperture feather must be in (0, 1].");
            }
            if (!float.IsFinite(Math.Max(worldFadeSeconds, contentDelaySeconds) + contentFadeSeconds))
            {
                throw new ArgumentException("The complete content transition must have a finite duration.");
            }
            WorldFadeSeconds = worldFadeSeconds;
            ContentDelaySeconds = contentDelaySeconds;
            ContentFadeSeconds = contentFadeSeconds;
            ExitFadeSeconds = exitFadeSeconds;
            IrisFeather = irisFeather;
            WorldPauseDelaySeconds = worldPauseDelaySeconds;
        }

        private static void RequireDuration(float seconds, string name)
        {
            if (!float.IsFinite(seconds) || seconds < 0)
            {
                throw new ArgumentOutOfRangeException(name, "A transition duration must be finite and non-negative.");
            }
        }
    }
}
