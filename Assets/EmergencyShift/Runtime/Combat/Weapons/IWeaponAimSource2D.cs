using UnityEngine;

namespace EmergencyShift
{
    public interface IWeaponAimSource2D
    {
        bool UsesTarget { get; }
        bool HasTarget { get; }
        Vector2 Target { get; }
        Vector2 FallbackDirection { get; }
    }
}
