using UnityEngine;

namespace EmergencyShift
{
    internal static class AudioProfileValidation
    {
        internal static bool Unit(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
        internal static bool AtLeast(float value, float minimum) => float.IsFinite(value) && value >= minimum;
        internal static bool Positive(float value) => float.IsFinite(value) && value > 0;
        internal static bool Color(Color color) => Unit(color.r) && Unit(color.g) && Unit(color.b) && Unit(color.a);
    }
}
