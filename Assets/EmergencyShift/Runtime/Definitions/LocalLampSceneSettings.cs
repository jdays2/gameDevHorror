using System;
using EmergencyShift.Features.LampPower.Config;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Inline scene/prefab tuning. Every placed fixture owns its own values.</summary>
    [Serializable]
    public sealed class LocalLampSceneSettings
    {
        [Header("Light")]
        [ColorUsage(false)]
        public Color color = new Color(.52f, .76f, .78f);
        [Min(.2f), Tooltip("World units; independent of the fixture length.")]
        public float lightRadius = 3.4f;
        [Min(0)]
        public float intensity = 1.2f;
        public bool castsShadows = true;
        public bool startsOn = true;
        public bool flickers = true;
        public float phase;
        [Header("Sound")]
        [Min(0), Tooltip("World units in XY. Zero mutes this fixture.")]
        public float soundRadius = 4;
        [Range(0, 1)]
        public float humVolume = .07f;
        [Range(0, 1)]
        public float contactVolume = .12f;
        [Header("Fixture")]
        [Min(.1f), Tooltip("Visible length along the wall, in world units. Keeps the back rail attached.")]
        public float fixtureLength = .75f;

        public float SoundGain(float distance, LampFlickerSettings curve = null)
        {
            if (!float.IsFinite(distance) || distance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distance));
            }

            if (soundRadius == 0)
            {
                return 0;
            }

            float fullVolumeFraction = (curve ?? LampFlickerSettings.LegacyDefaults).FullVolumeFraction;
            float progress = Mathf.InverseLerp(soundRadius * fullVolumeFraction, soundRadius, distance);
            return 1 - Mathf.SmoothStep(0, 1, progress);
        }

        public float Brightness(float clock, LampFlickerSettings curve = null) =>
            flickers ? (curve ?? LampFlickerSettings.LegacyDefaults).Evaluate(clock) : 1;

        public void Validate()
        {
            if (!float.IsFinite(lightRadius) || lightRadius < .2f ||
                !float.IsFinite(intensity) || intensity < 0 || !float.IsFinite(phase) ||
                !float.IsFinite(soundRadius) || soundRadius < 0 ||
                !float.IsFinite(humVolume) || humVolume < 0 || humVolume > 1 ||
                !float.IsFinite(contactVolume) || contactVolume < 0 || contactVolume > 1 ||
                !float.IsFinite(fixtureLength) || fixtureLength < .1f ||
                !float.IsFinite(color.r) || !float.IsFinite(color.g) || !float.IsFinite(color.b))
            {
                throw new InvalidOperationException("Invalid scene lamp settings.");
            }
        }
    }
}
