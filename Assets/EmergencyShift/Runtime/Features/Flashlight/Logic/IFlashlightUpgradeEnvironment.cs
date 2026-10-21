using EmergencyShift.Features.Flashlight.Config;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    /// <summary>The owning flashlight's live gate and optics; no global player or scene query.</summary>
    public interface IFlashlightUpgradeEnvironment
    {
        bool CanUpgrade { get; }
        Vector2 Optics { get; }
        FlashlightUpgradeLimits Limits { get; }
        void ApplyOptics(Vector2 optics);
    }
}
