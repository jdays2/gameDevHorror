using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Ground loot is collected by proximity; doors keep IInteractable/E.</summary>
    public interface IAutoPickup
    {
        Vector2 PickupPosition
        {
            get;
        }

        bool TryCollect(IInteractionActor actor);
    }
}
