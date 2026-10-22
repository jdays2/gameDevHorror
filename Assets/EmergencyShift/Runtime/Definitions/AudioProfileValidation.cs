using UnityEngine;

namespace EmergencyShift
{
    internal static class AudioProfileValidation
    {
        // Previous serialized defaults select the shared profile; intentional instance overrides win.
        internal static T Resolve<T>(T authored, T formerDefault, T shared, bool forceAuthored) =>
            forceAuthored || !System.Collections.Generic.EqualityComparer<T>.Default.Equals(authored, formerDefault) ? authored : shared;

        internal static bool Interval(Vector2 value) => Positive(value.x) && AtLeast(value.y, value.x);
        internal static bool Unit(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
        internal static bool AtLeast(float value, float minimum) => float.IsFinite(value) && value >= minimum;
        internal static bool Positive(float value) => float.IsFinite(value) && value > 0;
        internal static bool Color(Color color) => Unit(color.r) && Unit(color.g) && Unit(color.b) && Unit(color.a);
    }
}
