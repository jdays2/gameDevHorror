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
        [Header("Casing ejection — world units and degrees/second")]
        [SerializeField] private Vector2 casingPort = new Vector2(.16f, .16f);
        [SerializeField] private float casingWallClearance = .03f;
        [SerializeField] private Vector2 casingSideSpeed = new Vector2(1.2f, 1.8f);
        [SerializeField] private Vector2 casingForwardSpeed = new Vector2(-.25f, .4f);
        [SerializeField] private Vector2 casingRiseSpeed = new Vector2(1.35f, 1.85f);
        [SerializeField] private Vector2 casingSpin = new Vector2(-650, 650);


        public Vector2 CasingPort => casingPort;
        public float CasingWallClearance => casingWallClearance;
        public Vector2 CasingSideSpeed => casingSideSpeed;
        public Vector2 CasingForwardSpeed => casingForwardSpeed;
        public Vector2 CasingRiseSpeed => casingRiseSpeed;
        public Vector2 CasingSpin => casingSpin;
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

            if (!float.IsFinite(casingPort.x) || !float.IsFinite(casingPort.y) ||
                !float.IsFinite(casingWallClearance) || casingWallClearance < 0 ||
                !Ordered(casingSideSpeed) || !Ordered(casingForwardSpeed) || !Ordered(casingRiseSpeed) ||
                !Ordered(casingSpin) || casingSideSpeed.x < 0 || casingRiseSpeed.x < 0)
            {
                throw new InvalidOperationException("Invalid casing ejection: " + name);
            }

            projectile.Validate();
        }
        private static bool Ordered(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && value.y >= value.x;
    }
}
