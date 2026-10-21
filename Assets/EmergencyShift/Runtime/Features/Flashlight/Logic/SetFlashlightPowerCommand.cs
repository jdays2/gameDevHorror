using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class SetFlashlightPowerCommand : ICommand
    {
        private readonly FlashlightStateModel model;
        private readonly bool on;
        private readonly bool hasBeam;

        public SetFlashlightPowerCommand(FlashlightStateModel model, bool on, bool hasBeam)
        {
            this.model = model;
            this.on = on;
            this.hasBeam = hasBeam;
        }

        public CommandResult CanExecute() => model != null
            ? CommandResult.Ok() : CommandResult.Fail("Missing flashlight state.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.SetPower(on && hasBeam);
            }

            return result;
        }
    }
}
