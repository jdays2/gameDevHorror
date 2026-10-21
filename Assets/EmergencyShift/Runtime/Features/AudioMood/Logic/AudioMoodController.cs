using System;
using System.Collections.Generic;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public sealed class AudioMoodController
    {
        private readonly AudioMoodModel model;
        private readonly IAudioMoodView view;
        public bool ObservationDue => model.ObservationDue;
        public bool IsDucking => model.IsDucking;
        public float AwarenessDistance => model.AwarenessDistance;

        public AudioMoodController(AudioMoodModel model, IAudioMoodView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Observe(IReadOnlyList<AudioThreatObservation> sources, bool alive)
        {
            model.Observe(sources, alive);
            Present();
        }

        public CommandResult Shot() => new AudioMoodCueCommand(model, shot: true, remainingHealth: 0).Execute();
        public CommandResult Damage(int remainingHealth) => new AudioMoodCueCommand(model, shot: false, remainingHealth).Execute();
        public CommandResult Reset()
        {
            CommandResult result = new ResetAudioMoodCommand(model).Execute();
            if (result.Success)
            {
                Present();
            }

            return result;
        }

        private void Present() => view.ApplyMood(model.Mood, model.PerceivedEnemies, model.EngagedEnemies);
    }
}
