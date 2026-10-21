using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class SetFlashlightOpticsCommand : ICommand
    {
        private readonly FlashlightStateModel model;
        private readonly Vector2 optics;

        public SetFlashlightOpticsCommand(FlashlightStateModel model, Vector2 optics)
        {
            this.model = model;
            this.optics = optics;
        }

        public CommandResult CanExecute() => model == null
            ? CommandResult.Fail("Missing flashlight state.") : FlashlightStateModel.CanSetOptics(optics);

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.SetOptics(optics);
            }

            return result;
        }
    }
}
