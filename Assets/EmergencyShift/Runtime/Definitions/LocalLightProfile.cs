using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared light appearance; power and phase remain on each placed lamp.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Local light profile")]
    public sealed class LocalLightProfile : ScriptableObject
    {
        public Color color = new Color(.65f, .85f, 1);
        [Min(.2f)]
        public float radius = 1.5f;
        [Min(0)]
        public float intensity = .65f;
        public bool castsShadows = true;
        [Min(.1f)]
        public float cycleSeconds = 6;
        [Range(0, 1)]
        public float minimumFactor = .85f;
        [Min(0)]
        public float noiseFrequency = 1;
        [Tooltip("Dark intervals within the normalized cycle, from 0 to 1.")]
        public Vector2[] darkIntervals = Array.Empty<Vector2>();

        public float Evaluate(float clock)
        {
            float phase = Mathf.Repeat(clock / cycleSeconds, 1);
            foreach (var interval in darkIntervals)
            {
                if (phase >= interval.x && phase < interval.y)
                {
                    return 0;
                }
            }

            return Mathf.Lerp(minimumFactor, 1, Mathf.PerlinNoise(clock * noiseFrequency, 3.1f));
        }

        public void Validate()
        {
            if (!float.IsFinite(radius) || radius < .2f || !float.IsFinite(intensity) || intensity < 0 ||
                !float.IsFinite(cycleSeconds) || cycleSeconds < .1f ||
                !float.IsFinite(minimumFactor) || minimumFactor < 0 || minimumFactor > 1 ||
                !float.IsFinite(noiseFrequency) || noiseFrequency < 0 || darkIntervals == null)
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
