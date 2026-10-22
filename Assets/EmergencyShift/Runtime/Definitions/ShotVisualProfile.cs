using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Общие параметры одного визуального варианта. Не хранит патроны/состояние игрока.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Shot visual profile")]
    public sealed class ShotVisualProfile : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Тёплая вспышка";
        [SerializeField]
        private Color flashColor = new Color(1, 0.72f, 0.38f);
        [SerializeField]
        private ProjectileVisualProfile projectile;
        [Tooltip("Maximum muzzle lifetime; visible plume duration also limits the light.")]
        [Min(0.02f)]
        [SerializeField]
        private float flashSeconds = 0.095f;
        [Min(0.1f)]
        [SerializeField]
        private float flashRadius = 2.8f;
        [Min(0)]
        [SerializeField]
        private float flashIntensity = 8f;
        [Min(0.1f)]
        [SerializeField]
        private float muzzleSize = 1f;
        [Header("Visible fire plume (light ends with the plume)")]
        [Min(0.02f)]
        [SerializeField]
        private float plumeSeconds = .095f;
        [Min(0.1f)]
        [SerializeField]
        private float plumeWidth = .95f;
        [Min(0.1f)]
        [SerializeField]
        private float plumeLength = .78f;
        [Min(0.02f)]
        [SerializeField]
        private float impactSeconds = 0.12f;
        public string DisplayName => displayName;
        public Color FlashColor => flashColor;
        public ProjectileVisualProfile Projectile => projectile;
        public float FlashSeconds => flashSeconds;
        public float FlashRadius => flashRadius;
        public float FlashIntensity => flashIntensity;
        public float MuzzleSize => muzzleSize;
        public float PlumeSeconds => plumeSeconds;
        public float PlumeWidth => plumeWidth;
        public float PlumeLength => plumeLength;
        public float ImpactSeconds => impactSeconds;

        public void Validate()
        {
            if (projectile == null || !float.IsFinite(flashColor.r) || !float.IsFinite(flashColor.g) ||
                !float.IsFinite(flashColor.b) || !float.IsFinite(flashColor.a) || flashColor.a < 0 || flashColor.a > 1 ||
                !float.IsFinite(flashSeconds) || flashSeconds < .02f || !float.IsFinite(flashRadius) || flashRadius < .1f ||
                !float.IsFinite(flashIntensity) || flashIntensity < 0 || !float.IsFinite(muzzleSize) || muzzleSize < .1f ||
                !float.IsFinite(plumeSeconds) || plumeSeconds < .02f || !float.IsFinite(plumeWidth) || plumeWidth < .1f ||
                !float.IsFinite(plumeLength) || plumeLength < .1f || !float.IsFinite(impactSeconds) || impactSeconds < .02f)
            {
                throw new InvalidOperationException("Invalid shot presentation: " + name);
            }

            projectile.Validate();
        }
    }
}
