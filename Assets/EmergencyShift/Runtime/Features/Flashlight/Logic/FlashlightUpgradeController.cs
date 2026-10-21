using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class FlashlightUpgradeController
    {
        private readonly FlashlightUpgradeModel model;
        private readonly IFlashlightUpgradeEnvironment environment;

        public FlashlightUpgradeController(FlashlightUpgradeModel model, IFlashlightUpgradeEnvironment environment)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public int InstalledCount => model.InstalledCount;
        public bool HasModule(string id) => model.HasModule(id);
        public FlashlightModule[] Capture() => model.Capture();

        public bool CanImprove(Vector2 bonus) => environment.CanUpgrade &&
            model.CanImprove(bonus, environment.Optics, environment.Limits).Success;

        public CommandResult TryInstall(string id, Vector2 bonus, bool repeatable)
        {
            var request = new FlashlightModuleRequest(id, bonus, repeatable, environment.Optics,
                environment.Limits, environment.CanUpgrade);
            var command = new InstallFlashlightModuleCommand(model, request);
            CommandResult result = command.Execute();
            if (result.Success)
            {
                environment.ApplyOptics(command.Optics);
            }

            return result;
        }

        public CommandResult Restore(FlashlightModule[] modules, int count)
        {
            var command = new RestoreFlashlightModulesCommand(model, modules, count, environment.Limits);
            CommandResult result = command.Execute();
            if (result.Success)
            {
                environment.ApplyOptics(command.Optics);
            }

            return result;
        }
    }
}
