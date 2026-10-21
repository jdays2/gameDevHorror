using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood.Config;
using EmergencyShift.Features.AudioMood.Logic;

namespace EmergencyShift.Features.AudioMood
{
    public sealed class AudioThreatSignalControllerFactory
    {
        private readonly AudioThreatSignalSettings settings;
        private readonly IGameClock clock;
        public AudioThreatSignalControllerFactory(AudioThreatSignalSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public AudioThreatSignalController Create() => new AudioThreatSignalController(new AudioThreatSignalModel(settings, clock));
    }
}
