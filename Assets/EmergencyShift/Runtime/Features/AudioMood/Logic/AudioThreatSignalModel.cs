using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood.Config;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public sealed class AudioThreatSignalModel
    {
        private readonly AudioThreatSignalSettings settings;
        private readonly IGameClock clock;
        public float HeardUntil { get; private set; } = float.NegativeInfinity;
        public float UrgentUntil { get; private set; } = float.NegativeInfinity;

        public AudioThreatSignalModel(AudioThreatSignalSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public CommandResult CanReport(float seconds, float gain, bool alive) => Validate(seconds, gain, alive, clock.Time);

        private CommandResult Validate(float seconds, float gain, bool alive, double now)
        {
            if (!alive || !float.IsFinite(seconds) || !float.IsFinite(gain) || gain < settings.MinimumGain)
            {
                return CommandResult.Fail("Threat cue is dead, inaudible or invalid.");
            }

            if (!double.IsFinite(now) || now < 0 || now + Math.Max(settings.MaximumHeardSeconds, settings.UrgentSeconds) > float.MaxValue)
            {
                return CommandResult.Fail("Threat signal clock is invalid.");
            }

            return CommandResult.Ok();
        }

        internal CommandResult Report(float seconds, float gain, bool alive, bool urgent)
        {
            double sampledTime = clock.Time;
            CommandResult result = Validate(seconds, gain, alive, sampledTime);
            if (!result.Success)
            {
                return result;
            }

            float now = (float)sampledTime;
            float duration = Math.Clamp(seconds, settings.MinimumHeardSeconds, settings.MaximumHeardSeconds);
            HeardUntil = Math.Max(HeardUntil, now + duration);
            if (urgent)
            {
                UrgentUntil = now + settings.UrgentSeconds;
            }

            return result;
        }

        internal void Reset()
        {
            HeardUntil = float.NegativeInfinity;
            UrgentUntil = float.NegativeInfinity;
        }
    }
}
