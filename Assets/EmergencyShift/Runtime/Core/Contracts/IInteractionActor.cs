using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Native interaction port. Domain commands do not receive scene components.</summary>
    public interface IInteractionActor
    {
        bool CanReach(Vector2 point, float targetRange, Collider2D allowedBlocker = null);
        void ShowNotification(string message);
        void ReportPickup(PickupKind kind, string description);
    }
}
