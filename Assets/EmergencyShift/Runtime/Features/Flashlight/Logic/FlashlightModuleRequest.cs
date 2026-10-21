using EmergencyShift.Features.Flashlight.Config;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public readonly struct FlashlightModuleRequest
    {
        public FlashlightModuleRequest(string id, Vector2 bonus, bool repeatable, Vector2 currentOptics,
            FlashlightUpgradeLimits limits, bool available)
        {
            Id = id;
            Bonus = bonus;
            Repeatable = repeatable;
            CurrentOptics = currentOptics;
            Limits = limits;
            Available = available;
        }

        public string Id { get; }
        public Vector2 Bonus { get; }
        public bool Repeatable { get; }
        public Vector2 CurrentOptics { get; }
        public FlashlightUpgradeLimits Limits { get; }
        public bool Available { get; }
    }
}
