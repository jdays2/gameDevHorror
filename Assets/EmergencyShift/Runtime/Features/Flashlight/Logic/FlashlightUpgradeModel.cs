using System;
using System.Collections.Generic;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.Flashlight.Config;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    /// <summary>Personal progression; shared module IDs accumulate only when explicitly repeatable.</summary>
    public sealed class FlashlightUpgradeModel
    {
        private readonly Dictionary<string, Vector2> installed = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        private readonly Vector2 baseOptics;

        public FlashlightUpgradeModel(Vector2 baseOptics)
        {
            if (!ValidOptics(baseOptics))
            {
                throw new ArgumentException("Finite initial flashlight optics required.", nameof(baseOptics));
            }

            this.baseOptics = baseOptics;
        }

        public int InstalledCount { get; private set; }

        public bool HasModule(string id) => !string.IsNullOrWhiteSpace(id) && installed.ContainsKey(id);

        public CommandResult CanImprove(Vector2 bonus, Vector2 currentOptics, FlashlightUpgradeLimits limits)
        {
            if (!ValidBonus(bonus) || !ValidOptics(currentOptics) || !limits.IsValid || InstalledCount == int.MaxValue)
            {
                return CommandResult.Fail("Invalid module, optics, limits or exhausted install count.");
            }

            Vector2 improved = CalculateOptics(bonus, currentOptics, limits);
            return Mathf.Approximately(currentOptics.x, improved.x) && Mathf.Approximately(currentOptics.y, improved.y)
                ? CommandResult.Fail("The flashlight is already at these limits.") : CommandResult.Ok();
        }

        public CommandResult CanInstall(FlashlightModuleRequest request)
        {
            if (!request.Available || string.IsNullOrWhiteSpace(request.Id) || (!request.Repeatable && HasModule(request.Id)))
            {
                return CommandResult.Fail("Module is unavailable or already installed.");
            }

            installed.TryGetValue(request.Id, out Vector2 previous);
            if (!ValidBonus(previous + request.Bonus))
            {
                return CommandResult.Fail("Accumulated module bonus must remain finite.");
            }

            return CanImprove(request.Bonus, request.CurrentOptics, request.Limits);
        }

        public FlashlightModule[] Capture()
        {
            var result = new FlashlightModule[installed.Count];
            int index = 0;
            foreach (KeyValuePair<string, Vector2> item in installed)
            {
                result[index++] = new FlashlightModule(item.Key, item.Value);
            }

            return result;
        }

        public static CommandResult ValidateSnapshot(FlashlightModule[] modules, int count)
        {
            int uniqueCount = modules?.Length ?? 0;
            if (count < uniqueCount || (uniqueCount == 0 && count != 0))
            {
                return CommandResult.Fail("Invalid flashlight module count.");
            }

            if (modules == null)
            {
                return CommandResult.Ok();
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (FlashlightModule module in modules)
            {
                if (string.IsNullOrWhiteSpace(module.Id) || !ids.Add(module.Id) || !ValidBonus(module.Bonus))
                {
                    return CommandResult.Fail("Missing/duplicate flashlight module ID or invalid bonus.");
                }
            }

            return CommandResult.Ok();
        }

        internal Vector2 Install(FlashlightModuleRequest request)
        {
            Vector2 optics = CalculateOptics(request.Bonus, request.CurrentOptics, request.Limits);
            installed.TryGetValue(request.Id, out Vector2 previous);
            installed[request.Id] = previous + request.Bonus;
            InstalledCount++;
            return optics;
        }

        internal Vector2 Restore(FlashlightModule[] modules, int count, FlashlightUpgradeLimits limits)
        {
            installed.Clear();
            if (modules != null)
            {
                foreach (FlashlightModule module in modules)
                {
                    installed.Add(module.Id, module.Bonus);
                }
            }

            InstalledCount = count;
            Vector2 optics = Sum(Vector2.zero);
            return new Vector2(Mathf.Min(optics.x, Mathf.Max(baseOptics.x, limits.Range)),
                Mathf.Min(optics.y, Mathf.Max(baseOptics.y, limits.Angle)));
        }

        private Vector2 CalculateOptics(Vector2 bonus, Vector2 currentOptics, FlashlightUpgradeLimits limits)
        {
            Vector2 totals = Sum(bonus);
            // Lowering an authored cap must not make a new module downgrade the current optics.
            return new Vector2(Mathf.Max(currentOptics.x, Mathf.Min(totals.x, Mathf.Max(baseOptics.x, limits.Range))),
                Mathf.Max(currentOptics.y, Mathf.Clamp(totals.y, 20, Mathf.Max(baseOptics.y, limits.Angle))));
        }

        private Vector2 Sum(Vector2 bonus)
        {
            Vector2 total = baseOptics + bonus;
            foreach (Vector2 installedBonus in installed.Values)
            {
                total += installedBonus;
            }

            return total;
        }

        private static bool ValidBonus(Vector2 bonus) => float.IsFinite(bonus.x) && float.IsFinite(bonus.y) &&
            bonus.x >= 0 && bonus.y >= 0 && (bonus.x > 0 || bonus.y > 0);

        private static bool ValidOptics(Vector2 optics) => float.IsFinite(optics.x) && float.IsFinite(optics.y) &&
            optics.x >= .5f && optics.y >= 20 && optics.y <= 140;
    }
}
