using System;

namespace EmergencyShift.Features.Projectiles.Config
{
    public sealed class ProjectileFlightSettings
    {
        public ProjectileFlightSettings(float speed, float range, int damage, float radius, float maximumAge)
        {
            if (!float.IsFinite(speed) || speed < 1 || !float.IsFinite(range) || range < .1f || damage < 1 ||
                !float.IsFinite(radius) || radius < 0 || !float.IsFinite(maximumAge) || maximumAge <= 0)
            {
                throw new ArgumentException("Invalid projectile speed, range, damage, radius or lifetime.");
            }

            Speed = speed;
            Range = range;
            Damage = damage;
            Radius = radius;
            MaximumAge = maximumAge;
        }

        public float Speed { get; }
        public float Range { get; }
        public int Damage { get; }
        public float Radius { get; }
        public float MaximumAge { get; }
    }
}
