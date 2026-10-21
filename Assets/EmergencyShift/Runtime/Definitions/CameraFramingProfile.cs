using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Camera framing")]
    public sealed class CameraFramingProfile : ScriptableObject
    {
        [SerializeField, Min(0)]
        private float lightRangeMargin = .4f;
        [SerializeField, Min(.01f)]
        private float zoomSmoothTime = .35f;
        [SerializeField, Min(.5f), Tooltip("Half-height before a flashlight is found, in world units.")]
        private float unarmedViewRadius = 2.4f;

        public float LightRangeMargin => lightRangeMargin;
        public float ZoomSmoothTime => zoomSmoothTime;
        public float UnarmedViewRadius => unarmedViewRadius;

        public float OrthographicSize(float lightRange, float aspect, bool ownsFlashlight)
        {
            if (ownsFlashlight)
            {
                return OrthographicSize(lightRange, aspect);
            }

            Validate();
            if (!float.IsFinite(aspect) || aspect <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(aspect));
            }

            float size = unarmedViewRadius / Mathf.Min(1, aspect);
            if (!float.IsFinite(size) || size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(aspect));
            }

            return size;
        }

        public float OrthographicSize(float lightRange, float aspect)
        {
            Validate();
            if (!float.IsFinite(lightRange) || lightRange <= 0 || !float.IsFinite(aspect) || aspect <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lightRange), "Expected positive range and camera aspect.");
            }

            // Fit the beam into the shorter viewport dimension, including portrait windows.
            float size = (lightRange + lightRangeMargin) / Mathf.Min(1, aspect);
            if (!float.IsFinite(size) || size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lightRange));
            }

            return size;
        }

        public void Validate()
        {
            if (!float.IsFinite(lightRangeMargin) || lightRangeMargin < 0 ||
                !float.IsFinite(zoomSmoothTime) || zoomSmoothTime < .01f ||
                !float.IsFinite(unarmedViewRadius) || unarmedViewRadius < .5f)
            {
                throw new InvalidOperationException("Invalid camera framing: " + name);
            }
        }
    }
}
