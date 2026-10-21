using System;

namespace EmergencyShift.Features.AudioMood.Config
{
    public sealed class AudioThreatSignalSettings
    {
        public float MinimumGain { get; }
        public float MinimumHeardSeconds { get; }
        public float MaximumHeardSeconds { get; }
        public float UrgentSeconds { get; }

        public AudioThreatSignalSettings(float minimumGain, float minimumHeardSeconds, float maximumHeardSeconds, float urgentSeconds)
        {
            if (!float.IsFinite(minimumGain) || minimumGain < 0 || minimumGain > 1 ||
                !Positive(minimumHeardSeconds) || !Positive(maximumHeardSeconds) ||
                minimumHeardSeconds > maximumHeardSeconds || !Positive(urgentSeconds))
            {
                throw new ArgumentException("Invalid audio threat signal settings.");
            }

            MinimumGain = minimumGain;
            MinimumHeardSeconds = minimumHeardSeconds;
            MaximumHeardSeconds = maximumHeardSeconds;
            UrgentSeconds = urgentSeconds;
        }

        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
