using System;

namespace EmergencyShift.Features.DeathScreen.Config
{
    public sealed class DeathScreenSettings
    {
        public float WorldFadeSeconds { get; }
        public float ContentDelaySeconds { get; }
        public float ContentFadeSeconds { get; }
        public float ExitFadeSeconds { get; }
        public float EntrySeconds => Math.Max(WorldFadeSeconds, ContentDelaySeconds + ContentFadeSeconds);

        public DeathScreenSettings(float worldFadeSeconds, float contentDelaySeconds,
            float contentFadeSeconds, float exitFadeSeconds)
        {
            RequireDuration(worldFadeSeconds, nameof(worldFadeSeconds));
            RequireDuration(contentDelaySeconds, nameof(contentDelaySeconds));
            RequireDuration(contentFadeSeconds, nameof(contentFadeSeconds));
            RequireDuration(exitFadeSeconds, nameof(exitFadeSeconds));
            if (!float.IsFinite(contentDelaySeconds + contentFadeSeconds))
            {
                throw new ArgumentException("The complete content transition must have a finite duration.");
            }
            WorldFadeSeconds = worldFadeSeconds;
            ContentDelaySeconds = contentDelaySeconds;
            ContentFadeSeconds = contentFadeSeconds;
            ExitFadeSeconds = exitFadeSeconds;
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
