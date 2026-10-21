using System;
using EmergencyShift.Features.Flashlight.Config;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    /// <summary>Pure presentation interpolation; the environment alone writes the pivot.</summary>
    public sealed class FlashlightAimModel
    {
        private readonly FlashlightAimSettings settings;
        private float angularVelocity;

        public FlashlightAimModel(FlashlightAimSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool Step(float currentAngle, Vector2 offset, float deltaTime, out float angle)
        {
            angle = currentAngle;
            if (!Valid(currentAngle, offset) || !float.IsFinite(deltaTime) || deltaTime <= 0)
            {
                return false;
            }

            if (offset.sqrMagnitude < settings.DeadZone * settings.DeadZone)
            {
                angularVelocity = 0;
                return false;
            }

            float target = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90;
            angle = Mathf.SmoothDampAngle(currentAngle, target, ref angularVelocity,
                settings.SmoothTime, settings.MaximumSpeed, deltaTime);
            return true;
        }

        public bool Snap(float currentAngle, Vector2 offset, out float angle)
        {
            angle = currentAngle;
            if (!Valid(currentAngle, offset) || offset.sqrMagnitude <= .001f)
            {
                return false;
            }

            angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90;
            angularVelocity = 0;
            return true;
        }

        private static bool Valid(float angle, Vector2 offset) => float.IsFinite(angle) &&
            float.IsFinite(offset.x) && float.IsFinite(offset.y) && float.IsFinite(offset.sqrMagnitude);
    }
}
