using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public sealed class AudioLayerMixController
    {
        private readonly AudioLayerMixModel model;
        private readonly IAudioLayerMixView view;

        public AudioLayerMixController(AudioLayerMixModel model, IAudioLayerMixView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Step(float deltaTime, EmergencyShift.AudioMood mood, bool alive, bool wounded, bool ducking, float volume)
        {
            model.Step(deltaTime, mood, alive, wounded, ducking, volume);
            view.ApplyGains(model.Gains);
        }

        public CommandResult Reset()
        {
            CommandResult result = new ResetAudioLayerMixCommand(model).Execute();
            if (result.Success)
            {
                view.ApplyGains(model.Gains);
            }

            return result;
        }
    }
}
