using EmergencyShift.Features.Flashlight.Logic;
using EmergencyShift.Features.Flashlight.Config;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight
{
    public sealed class FlashlightControllerFactory
    {
        public FlashlightController Create(bool owned, bool on, Vector2 optics, IFlashlightView view)
        {
            return new FlashlightController(new FlashlightStateModel(owned, on && view != null && view.HasBeam, optics), view);
        }

        public FlashlightAimController CreateAim(FlashlightAimSettings settings, IFlashlightAimEnvironment environment)
        {
            return new FlashlightAimController(new FlashlightAimModel(settings), environment);
        }

        public FlashlightUpgradeController CreateUpgrades(Vector2 baseOptics, IFlashlightUpgradeEnvironment environment)
        {
            return new FlashlightUpgradeController(new FlashlightUpgradeModel(baseOptics), environment);
        }
    }
}
