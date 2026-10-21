using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public readonly struct RestorePickupCommand : ICommand
    {
        private readonly SingleUsePickupModel model;
        private readonly bool consumed;

        public RestorePickupCommand(SingleUsePickupModel model, bool consumed)
        {
            this.model = model;
            this.consumed = consumed;
        }

        public CommandResult CanExecute() => model != null && !model.Collecting
            ? CommandResult.Ok() : CommandResult.Fail("Cannot restore a pickup during its inventory effect.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(consumed);
            }

            return result;
        }
    }
}
