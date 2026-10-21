using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared light appearance; power and phase remain on each placed lamp.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Local light profile")]
    public sealed class LocalLightProfile : ScriptableObject
    {
        [SerializeField]
        private Color color = new Color(.65f, .85f, 1);
        [SerializeField, Min(.2f)]
        private float radius = 1.5f;
        [SerializeField, Min(0)]
        private float intensity = .65f;
        [SerializeField]
        private bool castsShadows = true;
        [SerializeField, Min(.1f)]
        private float cycleSeconds = 6;
        [SerializeField, Range(0, 1)]
        private float minimumFactor = .85f;
        [SerializeField, Min(0)]
        private float noiseFrequency = 1;
        [SerializeField, Tooltip("Dark intervals within the normalized cycle, from 0 to 1.")]
        private Vector2[] darkIntervals = Array.Empty<Vector2>();

        public Color Color => color;
        public float Radius => radius;
        public float Intensity => intensity;
        public bool CastsShadows => castsShadows;

        public float Evaluate(float clock)
        {
            if (!float.IsFinite(clock))
            {
                throw new ArgumentOutOfRangeException(nameof(clock));
            }

            float normalizedClock = clock / cycleSeconds;
            float noiseClock = clock * noiseFrequency;
            if (!float.IsFinite(normalizedClock) || !float.IsFinite(noiseClock))
            {
                throw new ArgumentOutOfRangeException(nameof(clock));
            }

            float phase = Mathf.Repeat(normalizedClock, 1);
            foreach (var interval in darkIntervals)
            {
                if (phase >= interval.x && phase < interval.y)
                {
                    return 0;
                }
            }

            return Mathf.Lerp(minimumFactor, 1, Mathf.PerlinNoise(noiseClock, 3.1f));
        }

        public void Validate()
        {
            if (!float.IsFinite(radius) || radius < .2f || !float.IsFinite(intensity) || intensity < 0 ||
                !float.IsFinite(cycleSeconds) || cycleSeconds < .1f ||
                !float.IsFinite(minimumFactor) || minimumFactor < 0 || minimumFactor > 1 ||
                !float.IsFinite(noiseFrequency) || noiseFrequency < 0 || darkIntervals == null ||
                !float.IsFinite(color.r) || !float.IsFinite(color.g) || !float.IsFinite(color.b) ||
                !float.IsFinite(color.a) || color.a < 0 || color.a > 1)
            {
                throw new InvalidOperationException("Invalid local light profile: " + name);
            }

            foreach (var interval in darkIntervals)
            {
                if (!float.IsFinite(interval.x) || !float.IsFinite(interval.y) ||
                    interval.x < 0 || interval.y > 1 || interval.x >= interval.y)
                {
                    throw new InvalidOperationException("Invalid light dropout interval: " + name);
                }
            }
        }
    }
}
