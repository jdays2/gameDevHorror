using UnityEngine;

namespace EmergencyShift.Core.Composition
{
    /// <summary>Scene-owned projection environment; gameplay lights are not camera providers.</summary>
    public sealed class CameraWorldProjection2D : IWorldProjection2D
    {
        private readonly Camera camera;

        public CameraWorldProjection2D(Camera camera) => this.camera = camera;

        public bool TryProjectViewportOffset(Vector2 worldOrigin, Vector2 worldOffset, out Vector2 viewportOffset)
        {
            viewportOffset = Vector2.zero;
            if (camera == null || !Finite(worldOrigin) || !Finite(worldOffset))
            {
                return false;
            }

            Vector2 destination = worldOrigin + worldOffset;
            if (!Finite(destination))
            {
                return false;
            }

            Vector3 origin = camera.WorldToViewportPoint(worldOrigin);
            Vector3 displaced = camera.WorldToViewportPoint(destination);
            viewportOffset = (Vector2)(displaced - origin);
            return Finite(viewportOffset);
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
