using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared recorded lamp sounds and near-field mix; playback state belongs to each lamp.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Local lamp audio set")]
    public sealed class LocalLampAudioSet : ScriptableObject
    {
        public AudioClip hum;
        public AudioClip[] contacts = Array.Empty<AudioClip>();
        [Range(0, 1)]
        public float humVolume = .07f;
        [Range(0, 1)]
        public float contactVolume = .12f;
        [Min(0)]
        public float fullVolumeDistance = .35f;
        [Min(.1f)]
        public float hearingDistance = 2;
        [Min(.01f)]
        public float referenceIntensity = .28f;
        [Min(0)]
        public float darkThreshold = .01f;
        [Min(0)]
        public float contactCooldown = .07f;
        [Min(.01f)]
        public float humFadeSeconds = .06f;

        public float DistanceGain(float distance)
        {
            if (!float.IsFinite(distance) || distance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distance));
            }

            float progress = Mathf.InverseLerp(fullVolumeDistance, hearingDistance, distance);
            return 1 - Mathf.SmoothStep(0, 1, progress);
        }

        public void Validate()
        {
            if (hum == null || contacts == null || contacts.Length == 0)
            {
                throw new InvalidOperationException("Lamp audio requires a hum loop and contact clips: " + name);
            }

            foreach (var contact in contacts)
            {
                if (contact == null)
                {
                    throw new InvalidOperationException("Missing lamp contact clip: " + name);
                }
            }

            if (!float.IsFinite(hearingDistance) || !float.IsFinite(fullVolumeDistance) ||
                fullVolumeDistance < 0 || hearingDistance <= fullVolumeDistance ||
                !float.IsFinite(referenceIntensity) || referenceIntensity <= 0 ||
                !float.IsFinite(darkThreshold) || darkThreshold < 0 || darkThreshold >= referenceIntensity ||
                !float.IsFinite(contactCooldown) || contactCooldown < 0 ||
                !float.IsFinite(humFadeSeconds) || humFadeSeconds <= 0 ||
                !float.IsFinite(humVolume) || humVolume < 0 || humVolume > 1 ||
                !float.IsFinite(contactVolume) || contactVolume < 0 || contactVolume > 1)
            {
                throw new InvalidOperationException("Invalid local lamp audio mix: " + name);
            }
        }
    }
}
