using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class AcquireFlashlightStateCommand : ICommand
    {
        private readonly FlashlightStateModel model;

        public AcquireFlashlightStateCommand(FlashlightStateModel model) => this.model = model;

        public CommandResult CanExecute() => model != null && !model.IsOwned
            ? CommandResult.Ok() : CommandResult.Fail("Flashlight is missing or already owned.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Acquire();
            }

            return result;
        }
    }
}
