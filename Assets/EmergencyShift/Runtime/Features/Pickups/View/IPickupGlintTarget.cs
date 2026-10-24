using UnityEngine;

namespace EmergencyShift.Features.Pickups.View
{
    /// <summary>Read-only acceptance, independent of the short automatic collection radius.</summary>
    public interface IPickupGlintTarget
    {
        Vector2 PickupPosition { get; }
        bool CanReceive(IPickupPreviewActor actor);
    }
}
