using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.WorldAudio.Logic
{
    public sealed class WorldAudioController : IDisposable
    {
        private readonly WorldAudioModel model;
        private readonly IWorldAudioGain output;
        private bool disposed;

        public float Gain => model.Gain;

        public WorldAudioController(WorldAudioModel model, IWorldAudioGain output)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public CommandResult SetDefeated(bool defeated)
        {
            if (disposed)
            {
                return CommandResult.Fail("World audio presentation is disposed.");
            }

            return new SetWorldAudioDefeatedCommand(model, defeated).Execute();
        }

        public void Step(float unscaledSeconds)
        {
            if (disposed)
            {
                return;
            }

            model.Step(unscaledSeconds);
            output.Apply(model.Gain);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            output.Dispose();
        }
    }
}
