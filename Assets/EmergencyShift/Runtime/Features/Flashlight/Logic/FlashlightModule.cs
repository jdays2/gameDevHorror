using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public readonly struct FlashlightModule
    {
        public FlashlightModule(string id, Vector2 bonus)
        {
            Id = id;
            Bonus = bonus;
        }

        public string Id { get; }
        public Vector2 Bonus { get; }
    }
}
