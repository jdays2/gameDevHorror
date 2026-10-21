using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Adapts the authored aim source and the legacy flashlight fallback.</summary>
    public sealed class WeaponAimSource2D : IWeaponAimSource2D
    {
        private readonly PlayerAim2D aim;
        private readonly Flashlight2D flashlight;
        public bool UsesTarget => aim != null;
        public bool HasTarget => aim != null && aim.HasTarget;
        public Vector2 Target => aim != null ? aim.WorldPoint : Vector2.zero;
        public Vector2 FallbackDirection => flashlight != null ? flashlight.Direction : Vector2.up;

        public WeaponAimSource2D(PlayerAim2D aim, Flashlight2D flashlight)
        {
            this.aim = aim;
            this.flashlight = flashlight;
        }
    }
}
