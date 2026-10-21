using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public readonly struct ResetThreatSignalCommand : ICommand
    {
        private readonly AudioThreatSignalModel model;
        public ResetThreatSignalCommand(AudioThreatSignalModel model) => this.model = model;
        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Threat signal model is missing.");
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
