using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Floor shadows")]
    public sealed class FloorShadowProfile : ScriptableObject
    {
        [Min(0)] public float lowLength = .6f;
        [Min(0)] public float furnitureLength = 1.4f;
        [Min(0)] public float tallLength = 2.8f;
        [Min(.01f)] public float tailFade = .16f;
        [Range(64, 512)] public int resolution = 256;

        public float Length(EnvironmentHeightClass height)
        {
            switch (height)
            {
                case EnvironmentHeightClass.Low:
                    return lowLength;
                case EnvironmentHeightClass.Furniture:
                    return furnitureLength;
                case EnvironmentHeightClass.Tall:
                    return tallLength;
                default:
                    throw new ArgumentOutOfRangeException(nameof(height));
            }
        }

        public void Validate()
        {
            if (!float.IsFinite(lowLength) || lowLength < 0 || !float.IsFinite(furnitureLength) ||
                furnitureLength < lowLength || !float.IsFinite(tallLength) || tallLength < furnitureLength ||
                !float.IsFinite(tailFade) || tailFade <= 0 || resolution < 64 || resolution > 512)
            {
                throw new InvalidOperationException("Invalid floor shadow profile: " + name);
            }
        }
    }
}
