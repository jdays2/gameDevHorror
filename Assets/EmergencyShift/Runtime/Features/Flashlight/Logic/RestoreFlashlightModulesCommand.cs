using EmergencyShift.Core.Commands;
using EmergencyShift.Features.Flashlight.Config;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class RestoreFlashlightModulesCommand : ICommand
    {
        private readonly FlashlightUpgradeModel model;
        private readonly FlashlightModule[] modules;
        private readonly int count;
        private readonly FlashlightUpgradeLimits limits;

        public RestoreFlashlightModulesCommand(FlashlightUpgradeModel model, FlashlightModule[] modules,
            int count, FlashlightUpgradeLimits limits)
        {
            this.model = model;
            this.modules = modules == null ? null : (FlashlightModule[])modules.Clone();
            this.count = count;
            this.limits = limits;
        }

        public Vector2 Optics { get; private set; }

        public CommandResult CanExecute() => model == null || !limits.IsValid
            ? CommandResult.Fail("Missing upgrade model or valid limits.")
            : FlashlightUpgradeModel.ValidateSnapshot(modules, count);

        public CommandResult Execute()
        {
            Optics = default;
            CommandResult result = CanExecute();
            if (result.Success)
            {
                Optics = model.Restore(modules, count, limits);
            }

            return result;
        }
    }
}
