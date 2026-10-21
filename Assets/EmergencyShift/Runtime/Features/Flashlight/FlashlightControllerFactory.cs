using EmergencyShift.Features.Flashlight.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight
{
    public sealed class FlashlightControllerFactory
    {
        public FlashlightUpgradeController CreateUpgrades(Vector2 baseOptics, IFlashlightUpgradeEnvironment environment)
        {
            return new FlashlightUpgradeController(new FlashlightUpgradeModel(baseOptics), environment);
        }
    }
}
