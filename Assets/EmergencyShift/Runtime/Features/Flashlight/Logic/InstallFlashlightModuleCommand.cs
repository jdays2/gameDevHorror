using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class InstallFlashlightModuleCommand : ICommand
    {
        private readonly FlashlightUpgradeModel model;
        private readonly FlashlightModuleRequest request;

        public InstallFlashlightModuleCommand(FlashlightUpgradeModel model, FlashlightModuleRequest request)
        {
            this.model = model;
            this.request = request;
        }

        public Vector2 Optics { get; private set; }

        public CommandResult CanExecute() => model?.CanInstall(request) ?? CommandResult.Fail("Missing upgrade model.");

        public CommandResult Execute()
        {
            Optics = default;
            CommandResult result = CanExecute();
            if (result.Success)
            {
                Optics = model.Install(request);
            }

            return result;
        }
    }
}
