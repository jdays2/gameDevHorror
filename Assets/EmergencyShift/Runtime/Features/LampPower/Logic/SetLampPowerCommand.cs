using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.LampPower.Logic
{
    public readonly struct SetLampPowerCommand : ICommand
    {
        private readonly LampPowerModel model;
        private readonly bool on;

        public SetLampPowerCommand(LampPowerModel model, bool on)
        {
            this.model = model;
            this.on = on;
        }

        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Missing lamp model.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.SetPower(on);
            }

            return result;
        }
    }
}
