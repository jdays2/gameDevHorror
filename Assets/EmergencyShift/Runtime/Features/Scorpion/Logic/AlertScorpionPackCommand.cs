using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Scorpion.Logic
{
    public sealed class AlertScorpionPackCommand : ICommand
    {
        private readonly ScorpionPackModel model;
        public AlertScorpionPackCommand(ScorpionPackModel model) => this.model = model;
        public CommandResult CanExecute() => model != null && !model.IsAlerted ? CommandResult.Ok() : CommandResult.Fail("Missing or already alerted pack.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Alert();
            }
            return result;
        }
    }
}
