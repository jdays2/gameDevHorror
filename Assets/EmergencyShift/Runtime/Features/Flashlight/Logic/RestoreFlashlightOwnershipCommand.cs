using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class RestoreFlashlightOwnershipCommand : ICommand
    {
        private readonly FlashlightStateModel model;
        private readonly bool owned;

        public RestoreFlashlightOwnershipCommand(FlashlightStateModel model, bool owned)
        {
            this.model = model;
            this.owned = owned;
        }

        public CommandResult CanExecute() => model != null
            ? CommandResult.Ok() : CommandResult.Fail("Missing flashlight state.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.RestoreOwnership(owned);
            }

            return result;
        }
    }
}
