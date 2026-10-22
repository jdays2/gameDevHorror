using System;
using UnityEngine;

namespace EmergencyShift.Features.LampPower.Config
{
    /// <summary>Immutable appearance curve. Power remains in the personal lamp model.</summary>
    public sealed class LampFlickerSettings
    {
        public static readonly LampFlickerSettings LegacyDefaults = new LampFlickerSettings(
            6.4f, .82f, 1.4f, new Vector2(4.9f, 5.02f), new Vector2(5.16f, 5.25f), .175f);
        public float CycleSeconds { get; }
        public float MinimumFactor { get; }
        public float NoiseFrequency { get; }
        public Vector2 FirstDarkInterval { get; }
        public Vector2 SecondDarkInterval { get; }
        public float FullVolumeFraction { get; }

        public LampFlickerSettings(float cycleSeconds, float minimumFactor, float noiseFrequency,
            Vector2 firstDarkInterval, Vector2 secondDarkInterval, float fullVolumeFraction)
        {
            if (!float.IsFinite(cycleSeconds) || cycleSeconds <= 0 ||
                !float.IsFinite(minimumFactor) || minimumFactor < 0 || minimumFactor > 1 ||
                !float.IsFinite(noiseFrequency) || noiseFrequency < 0 ||
                !ValidInterval(firstDarkInterval, cycleSeconds) || !ValidInterval(secondDarkInterval, cycleSeconds) ||
                !float.IsFinite(fullVolumeFraction) || fullVolumeFraction < 0 || fullVolumeFraction >= 1)
            {
                throw new ArgumentException("Invalid lamp flicker curve.");
            }

            CycleSeconds = cycleSeconds;
            MinimumFactor = minimumFactor;
            NoiseFrequency = noiseFrequency;
            FirstDarkInterval = firstDarkInterval;
            SecondDarkInterval = secondDarkInterval;
            FullVolumeFraction = fullVolumeFraction;
        }

        public float Evaluate(float clock)
        {
            float noiseClock = clock * NoiseFrequency;
            if (!float.IsFinite(clock) || !float.IsFinite(noiseClock))
            {
                throw new ArgumentOutOfRangeException(nameof(clock));
            }

            float position = Mathf.Repeat(clock, CycleSeconds);
            if (Inside(position, FirstDarkInterval) || Inside(position, SecondDarkInterval))
            {
                return 0;
            }

            return Mathf.Lerp(MinimumFactor, 1, Mathf.PerlinNoise(noiseClock, 3.1f));
        }

        private static bool Inside(float value, Vector2 interval) => value > interval.x && value < interval.y;
        private static bool ValidInterval(Vector2 value, float cycle) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && value.x >= 0 && value.y >= value.x && value.y <= cycle;
    }
}
