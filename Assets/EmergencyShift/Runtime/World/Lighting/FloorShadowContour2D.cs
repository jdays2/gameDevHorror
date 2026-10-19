using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Offline outline with a cached world-space copy. No texture reads or outline extraction in Play.</summary>
    [Serializable]
    public sealed class FloorShadowContour2D
    {
        public ShadowCaster2D caster;
        public Transform frame;
        public Vector2[] points = Array.Empty<Vector2>();
        [NonSerialized] private Vector2[] worldPoints;
        [NonSerialized] private Matrix4x4 previous;
        [NonSerialized] private Bounds bounds;

        public bool Active => frame != null && caster != null && caster.isActiveAndEnabled && caster.castsShadows;
        public Bounds WorldBounds
        {
            get
            {
                UpdateWorldPoints();
                return bounds;
            }
        }

        public Vector2[] WorldPoints
        {
            get
            {
                UpdateWorldPoints();
                return worldPoints;
            }
        }

        private void UpdateWorldPoints()
        {
            Matrix4x4 matrix = frame.localToWorldMatrix;
            if (worldPoints != null && worldPoints.Length == points.Length && previous == matrix)
            {
                return;
            }

            if (worldPoints == null || worldPoints.Length != points.Length)
            {
                worldPoints = new Vector2[points.Length];
            }

            previous = matrix;
            bounds = new Bounds(frame.position, Vector3.zero);
            for (int i = 0; i < points.Length; i++)
            {
                worldPoints[i] = frame.TransformPoint(points[i]);
                if (i == 0)
                {
                    bounds = new Bounds(worldPoints[i], Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(worldPoints[i]);
                }
            }
        }
    }
}
