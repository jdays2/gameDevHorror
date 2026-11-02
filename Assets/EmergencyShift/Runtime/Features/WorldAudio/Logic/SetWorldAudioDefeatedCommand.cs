using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.WorldAudio.Logic
{
    public readonly struct SetWorldAudioDefeatedCommand : ICommand
    {
        private readonly WorldAudioModel model;
        private readonly bool defeated;

        public SetWorldAudioDefeatedCommand(WorldAudioModel model, bool defeated)
        {
            this.model = model;
            this.defeated = defeated;
        }

        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("World audio model is missing.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.SetDefeated(defeated);
            }

            return result;
        }
    }
}
