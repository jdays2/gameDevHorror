using System;

namespace EmergencyShift.Features.Doors.Config
{
    public sealed class DoorRulesSettings
    {
        public const float LegacyDuration = .3f;
        public const float LegacyRange = 1.6f;
        public static readonly DoorRulesSettings LegacyDefaults = new DoorRulesSettings(LegacyDuration, LegacyRange);
        public float Duration { get; }
        public float InteractionRange { get; }

        public DoorRulesSettings(float duration, float interactionRange)
        {
            if (!float.IsFinite(duration) || duration < .05f || !float.IsFinite(interactionRange) || interactionRange <= 0)
            {
                throw new ArgumentException("Invalid door rules.");
            }

            Duration = duration;
            InteractionRange = interactionRange;
        }

        public float ProjectDuration(float authored, bool forceAuthored) =>
            forceAuthored || authored != LegacyDuration ? authored : Duration;
    }
}
