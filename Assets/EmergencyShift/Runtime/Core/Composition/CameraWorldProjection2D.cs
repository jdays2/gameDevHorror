using UnityEngine;

namespace EmergencyShift.Core.Composition
{
    /// <summary>Scene-owned projection environment; gameplay lights are not camera providers.</summary>
    public sealed class CameraWorldProjection2D : IWorldProjection2D, ICameraImpactView
    {
        private readonly Camera camera;
        private readonly CameraFollow2D follow;

        public CameraWorldProjection2D(Camera camera)
        {
            this.camera = camera;
            follow = camera != null ? camera.GetComponent<CameraFollow2D>() : null;
        }

        public void SetImpactOffset(Vector2 screenOffset)
        {
            if (follow != null)
            {
                follow.SetImpactOffset(screenOffset);
            }
        }

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
