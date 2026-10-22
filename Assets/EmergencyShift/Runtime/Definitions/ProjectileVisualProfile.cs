using System;
using UnityEngine;
using EmergencyShift.Features.Projectiles.Config;

namespace EmergencyShift
{
    /// <summary>One shared presentation for flying bullets and pellets; no volley or weapon state.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Projectile visual profile")]
    public sealed class ProjectileVisualProfile : ScriptableObject
    {
        [SerializeField]
        private Color bulletColor = new Color(.8f, .74f, .62f, .25f);
        [Min(1)]
        [SerializeField]
        private float bulletSpeed = 44;
        [Min(.02f)]
        [SerializeField]
        private float trailLength = .12f;
        [Min(.01f)]
        [SerializeField]
        private float trailWidth = .012f;
        [Range(0, 1)]
        [SerializeField]
        private float projectileVisibility = 1;
        [Range(0, .8f)]
        [SerializeField]
        private float trailVariation = .45f;
        [Min(0)]
        [SerializeField]
        private float bulletLightRadius;
        [Min(0)]
        [SerializeField]
        private float bulletLightIntensity;
        [Header("Flight compatibility settings")]
        [SerializeField, Min(0)]
        private float collisionRadius = .035f;
        [SerializeField, Min(.01f)]
        private float maximumFlightSeconds = 3;

        public Color BulletColor => bulletColor;
        public float BulletSpeed => bulletSpeed;
        public float TrailLength => trailLength;
        public float TrailWidth => trailWidth;
        public float ProjectileVisibility => projectileVisibility;
        public float TrailVariation => trailVariation;
        public float BulletLightRadius => bulletLightRadius;
        public float BulletLightIntensity => bulletLightIntensity;

        public ProjectileFlightSettings ToFlightSettings(float range, int damage)
        {
            Validate();
            if (!float.IsFinite(range))
            {
                throw new ArgumentException("Projectile range must be finite.", nameof(range));
            }

            return new ProjectileFlightSettings(bulletSpeed, Mathf.Max(.1f, range), damage, collisionRadius, maximumFlightSeconds);
        }

        public void Validate()
        {
            if (!float.IsFinite(bulletSpeed) || bulletSpeed < 1 ||
                !float.IsFinite(trailLength) || trailLength < .02f ||
                !float.IsFinite(trailWidth) || trailWidth < .01f ||
                !float.IsFinite(projectileVisibility) || projectileVisibility < 0 || projectileVisibility > 1 ||
                !float.IsFinite(trailVariation) || trailVariation < 0 || trailVariation > .8f ||
                !float.IsFinite(bulletLightRadius) || bulletLightRadius < 0 ||
                !float.IsFinite(bulletLightIntensity) || bulletLightIntensity < 0 ||
                !float.IsFinite(bulletColor.r) || !float.IsFinite(bulletColor.g) || !float.IsFinite(bulletColor.b) ||
                !float.IsFinite(bulletColor.a) || bulletColor.a < 0 || bulletColor.a > 1 ||
                !float.IsFinite(collisionRadius) || collisionRadius < 0 ||
                !float.IsFinite(maximumFlightSeconds) || maximumFlightSeconds <= 0)
            {
                throw new InvalidOperationException("Invalid projectile presentation: " + name);
            }
        }
    }
}
