using System;
using System.IO;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Value-only guards shared by checkpoint poses and settled presentation snapshots.</summary>
    public static class CheckpointValueValidation
    {
        // Unity rotations and their JSON round-trips are unit quaternions within float precision.
        // Accept either sign; reject corrupt data instead of silently changing the saved pose.
        private const double UnitRotationTolerance = .001;

        public static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        public static bool IsFinite(Color value)
        {
            return float.IsFinite(value.r) && float.IsFinite(value.g) &&
                float.IsFinite(value.b) && float.IsFinite(value.a);
        }

        public static bool IsUnitRotation(Quaternion value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w))
            {
                return false;
            }

            double squaredLength = (double)value.x * value.x + (double)value.y * value.y +
                (double)value.z * value.z + (double)value.w * value.w;
            return Math.Abs(squaredLength - 1) <= UnitRotationTolerance;
        }

        public static void ValidatePose(Vector3 position, Quaternion rotation)
        {
            if (!IsFinite(position))
            {
                throw new InvalidDataException("Invalid checkpoint position");
            }

            if (!IsUnitRotation(rotation))
            {
                throw new InvalidDataException("Invalid checkpoint rotation: expected a finite unit quaternion");
            }
        }
    }
}
