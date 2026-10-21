using System;
using UnityEngine;

namespace EmergencyShift.Features.AudioMood.Config
{
    public sealed class AudioLayerMixSettings
    {
        public float MasterVolume { get; }
        public Vector3 LayerGains { get; }
        public float RiseSeconds { get; }
        public float FallSeconds { get; }
        public float DeathFadeSeconds { get; }
        public float DuckGain { get; }
        public float DuckResponseSeconds { get; }
        public float LowHealthGain { get; }

        public AudioLayerMixSettings(float masterVolume, Vector3 layerGains, float riseSeconds, float fallSeconds,
            float deathFadeSeconds, float duckGain, float duckResponseSeconds, float lowHealthGain)
        {
            if (!Unit(masterVolume) || !Unit(layerGains.x) || !Unit(layerGains.y) || !Unit(layerGains.z) ||
                !AtLeast(riseSeconds, .05f) || !AtLeast(fallSeconds, .05f) || !AtLeast(deathFadeSeconds, .05f) ||
                !Unit(duckGain) || !AtLeast(duckResponseSeconds, .02f) || !Unit(lowHealthGain))
            {
                throw new ArgumentException("Invalid audio layer mix settings.");
            }

            MasterVolume = masterVolume;
            LayerGains = layerGains;
            RiseSeconds = riseSeconds;
            FallSeconds = fallSeconds;
            DeathFadeSeconds = deathFadeSeconds;
            DuckGain = duckGain;
            DuckResponseSeconds = duckResponseSeconds;
            LowHealthGain = lowHealthGain;
        }

        private static bool Unit(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
        private static bool AtLeast(float value, float minimum) => float.IsFinite(value) && value >= minimum;
    }
}
