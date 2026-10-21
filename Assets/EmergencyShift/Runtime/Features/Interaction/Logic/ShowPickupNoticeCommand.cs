using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Interaction.Logic
{
    public readonly struct ShowPickupNoticeCommand : ICommand
    {
        private readonly PickupNoticeModel model;
        private readonly string message;

        public ShowPickupNoticeCommand(PickupNoticeModel model, string message)
        {
            this.model = model;
            this.message = message;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && !string.IsNullOrEmpty(message)
            ? CommandResult.Ok() : CommandResult.Fail("Notice requires a message and valid personal clock.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Show(message);
            }

            return result;
        }
    }
}
