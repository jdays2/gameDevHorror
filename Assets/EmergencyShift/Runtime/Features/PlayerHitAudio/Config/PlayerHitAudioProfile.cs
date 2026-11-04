using System;
using UnityEngine;

namespace EmergencyShift.Features.PlayerHitAudio.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Audio/Player hit profile")]
    public sealed class PlayerHitAudioProfile : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField, Range(0, 1)] private float volume = .8f;
        [SerializeField, Range(0, .05f)] private float pitchVariation = .012f;
        [SerializeField, Min(.005f)] private float overlapFadeSeconds = .018f;

        public AudioClipBank Clips => clips;
        public float Volume => volume;
        public float PitchVariation => pitchVariation;
        public float OverlapFadeSeconds => overlapFadeSeconds;

        public void Validate()
        {
            if (clips == null || clips.Length == 0 ||
                !AudioProfileValidation.Unit(volume) ||
                !float.IsFinite(pitchVariation) || pitchVariation < 0 || pitchVariation > .05f ||
                !float.IsFinite(overlapFadeSeconds) || overlapFadeSeconds < .005f || overlapFadeSeconds > .1f)
            {
                throw new InvalidOperationException("Invalid player hit audio profile: " + name);
            }

            for (int index = 0; index < clips.Length; index++)
            {
                if (clips[index] == null)
                {
                    throw new InvalidOperationException("Missing player hit audio clip: " + name);
                }
                for (int previous = 0; previous < index; previous++)
                {
                    if (clips[index] == clips[previous])
                    {
                        throw new InvalidOperationException("Repeated player hit clip reference: " + name);
                    }
                }
            }
        }
    }
}
