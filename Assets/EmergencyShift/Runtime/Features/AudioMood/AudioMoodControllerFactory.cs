using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood.Config;
using EmergencyShift.Features.AudioMood.Logic;

namespace EmergencyShift.Features.AudioMood
{
    public sealed class AudioMoodControllerFactory
    {
        private readonly IGameClock clock;
        public AudioMoodControllerFactory(IGameClock clock) => this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        public AudioMoodController Create(AudioMoodSettings settings, IAudioMoodView view) =>
            new AudioMoodController(new AudioMoodModel(settings, clock), view);
    }
}
