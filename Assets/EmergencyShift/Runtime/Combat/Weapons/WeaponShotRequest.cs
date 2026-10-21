using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Environment result. No collider, camera, scene object or presentation reference.</summary>
    public readonly struct WeaponShotRequest
    {
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public Vector2 Target { get; }
        public bool MuzzleBlocked { get; }
        public bool IsValid => Finite(Origin) && Finite(Direction) && Finite(Target)
            && float.IsFinite(Direction.sqrMagnitude) && Direction.sqrMagnitude > .000001f;

        public WeaponShotRequest(Vector2 origin, Vector2 direction, Vector2 target, bool muzzleBlocked)
        {
            Origin = origin;
            Direction = direction;
            Target = target;
            MuzzleBlocked = muzzleBlocked;
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
