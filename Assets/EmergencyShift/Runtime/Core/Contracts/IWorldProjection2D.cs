using UnityEngine;

namespace EmergencyShift
{
    public interface IWorldProjection2D
    {
        bool TryProjectViewportOffset(Vector2 worldOrigin, Vector2 worldOffset, out Vector2 viewportOffset);
    }
}
