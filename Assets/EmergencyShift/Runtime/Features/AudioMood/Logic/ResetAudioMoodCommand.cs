using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public readonly struct ResetAudioMoodCommand : ICommand
    {
        private readonly AudioMoodModel model;
        public ResetAudioMoodCommand(AudioMoodModel model) => this.model = model;
        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Audio mood model is missing.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Reset();
            }

            return result;
        }
    }
}
