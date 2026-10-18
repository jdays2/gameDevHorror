using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Camera framing")]
    public sealed class CameraFramingProfile : ScriptableObject
    {
        [Min(0)]
        public float lightRangeMargin = .4f;
        [Min(.01f)]
        public float zoomSmoothTime = .35f;

        public float OrthographicSize(float lightRange, float aspect)
        {
            Validate();
            if (!float.IsFinite(lightRange) || lightRange <= 0 || !float.IsFinite(aspect) || aspect <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lightRange), "Expected positive range and camera aspect.");
            }

            // Fit the beam into the shorter viewport dimension, including portrait windows.
            return (lightRange + lightRangeMargin) / Mathf.Min(1, aspect);
        }

        public void Validate()
        {
            if (!float.IsFinite(lightRangeMargin) || lightRangeMargin < 0 ||
                !float.IsFinite(zoomSmoothTime) || zoomSmoothTime < .01f)
            {
                throw new InvalidOperationException("Invalid camera framing: " + name);
            }
        }
    }
}
