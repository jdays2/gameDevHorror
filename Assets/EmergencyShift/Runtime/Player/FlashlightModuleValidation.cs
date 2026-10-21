using System;
using EmergencyShift.Features.Flashlight.Logic;

namespace EmergencyShift
{
    /// <summary>Legacy JSON adapter; the pure progression validator owns all module rules.</summary>
    public static class FlashlightModuleValidation
    {
        public static void Validate(FlashlightUpgrades.ModuleState[] modules, int count)
        {
            var result = FlashlightUpgradeModel.ValidateSnapshot(ToModules(modules), count);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        public static FlashlightModule[] ToModules(FlashlightUpgrades.ModuleState[] modules)
        {
            if (modules == null)
            {
                return null;
            }

            var result = new FlashlightModule[modules.Length];
            for (int index = 0; index < modules.Length; index++)
            {
                FlashlightUpgrades.ModuleState module = modules[index];
                result[index] = module == null ? default : new FlashlightModule(module.id, module.bonus);
            }

            return result;
        }
    }
}