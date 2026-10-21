using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public readonly struct ResetAudioLayerMixCommand : ICommand
    {
        private readonly AudioLayerMixModel model;
        public ResetAudioLayerMixCommand(AudioLayerMixModel model) => this.model = model;
        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Audio layer mix model is missing.");
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
