using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared recorded lamp sounds and near-field mix; playback state belongs to each lamp.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Local lamp audio set")]
    public sealed class LocalLampAudioSet : ScriptableObject
    {
        [SerializeField]
        private AudioClip hum;
        [SerializeField]
        private AudioClip[] contacts = Array.Empty<AudioClip>();
        [SerializeField, Range(0, 1)]
        private float humVolume = .07f;
        [SerializeField, Range(0, 1)]
        private float contactVolume = .12f;
        [SerializeField, Min(0)]
        private float fullVolumeDistance = .7f;
        [SerializeField, Min(.1f)]
        private float hearingDistance = 4;
        [SerializeField, Min(.01f)]
        private float referenceIntensity = .28f;
        [SerializeField, Min(0)]
        private float darkThreshold = .01f;
        [SerializeField, Min(0)]
        private float contactCooldown = .07f;
        [SerializeField, Min(.01f)]
        private float humFadeSeconds = .06f;
        [SerializeField, Range(0, 1)]
        private float maximumPan = .5f;

        public AudioClip Hum => hum;
        public int ContactCount => contacts != null ? contacts.Length : 0;
        public float HumVolume => humVolume;
        public float ContactVolume => contactVolume;
        public float HearingDistance => hearingDistance;
        public float ReferenceIntensity => referenceIntensity;
        public float DarkThreshold => darkThreshold;
        public float ContactCooldown => contactCooldown;
        public float HumFadeSeconds => humFadeSeconds;
        public float MaximumPan => maximumPan;

        public AudioClip ContactAt(int index)
        {
            if (index < 0 || index >= ContactCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return contacts[index];
        }

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
                !float.IsFinite(contactVolume) || contactVolume < 0 || contactVolume > 1 ||
                !float.IsFinite(maximumPan) || maximumPan < 0 || maximumPan > 1)
            {
                throw new InvalidOperationException("Invalid local lamp audio mix: " + name);
            }
        }
    }
}
