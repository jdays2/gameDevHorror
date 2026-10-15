using UnityEngine;

namespace EmergencyShift
{
    public enum PickupKind
    {
        Ammo,
        Weapon,
        FlashlightUpgrade,
        AccessCard
    }

    /// <summary>Ground loot is collected by proximity; doors keep IInteractable/E.</summary>
    public interface IAutoPickup
    {
        Vector2 PickupPosition
        {
            get;
        }

        bool TryCollect(PlayerInteraction actor);
    }
}
