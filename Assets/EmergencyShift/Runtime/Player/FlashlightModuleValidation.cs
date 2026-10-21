using System;
using System.Collections.Generic;

namespace EmergencyShift
{
    /// <summary>Checks serialized module values before a player or module collection is changed.</summary>
    public static class FlashlightModuleValidation
    {
        public static void Validate(FlashlightUpgrades.ModuleState[] modules, int count)
        {
            int uniqueCount = modules == null ? 0 : modules.Length;
            // A repeatable module accumulates its bonuses under one ID, but counts every install.
            if (count < uniqueCount || (uniqueCount == 0 && count != 0))
            {
                throw new InvalidOperationException("Invalid flashlight module count");
            }

            if (modules == null)
            {
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (FlashlightUpgrades.ModuleState module in modules)
            {
                if (module == null || string.IsNullOrWhiteSpace(module.id) || !ids.Add(module.id))
                {
                    throw new InvalidOperationException("Missing or duplicate flashlight module ID");
                }

                float range = module.bonus.x;
                float angle = module.bonus.y;
                if (!float.IsFinite(range) || !float.IsFinite(angle) || range < 0 || angle < 0 ||
                    (range == 0 && angle == 0))
                {
                    throw new InvalidOperationException("Invalid flashlight module bonus");
                }
            }
        }
    }
}
