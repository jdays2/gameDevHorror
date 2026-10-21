using UnityEngine;

namespace EmergencyShift.Features.Flashlight.View
{
    /// <summary>Physics environment only; coverage and power decisions belong to the model.</summary>
    public sealed class FlashlightObstruction2D
    {
        public bool IsUnblocked(Vector2 origin, Vector2 point, LayerMask geometry) =>
            Physics2D.Linecast(origin, point, geometry).collider == null;
    }
}
