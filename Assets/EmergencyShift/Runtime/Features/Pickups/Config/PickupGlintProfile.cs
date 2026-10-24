using System;
using UnityEngine;

namespace EmergencyShift.Features.Pickups.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Pickup Glint")]
    public sealed class PickupGlintProfile : ScriptableObject
    {
        [SerializeField] private Material material;
        [SerializeField] private Color color = new Color(.85f, .85f, .78f, 1);
        [SerializeField, Range(0, 1)] private float intensity = .78f;
        [SerializeField, Min(.05f)] private float duration = .6f;
        [SerializeField, Min(0)] private float rearmSeconds = .65f;
        [SerializeField, Range(.02f, .6f)] private float stripeWidth = .2f;

        public Material Material => material;
        public Color Color => color;
        public float Intensity => intensity;
        public float Duration => duration;
        public float RearmSeconds => rearmSeconds;
        public float StripeWidth => stripeWidth;

        public void Validate()
        {
            if (material == null || !Finite(duration) || duration <= 0 || !Finite(rearmSeconds) || rearmSeconds < 0 ||
                !Finite(intensity) || intensity < 0 || intensity > 1 || !Finite(stripeWidth) || stripeWidth <= 0 || stripeWidth > 1 ||
                !Finite(color.r) || !Finite(color.g) || !Finite(color.b) || !Finite(color.a))
            {
                throw new InvalidOperationException("Pickup glint requires a material and finite presentation settings.");
            }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
