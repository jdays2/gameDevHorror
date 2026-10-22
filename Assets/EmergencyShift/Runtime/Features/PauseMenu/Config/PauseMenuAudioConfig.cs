using System;
using UnityEngine;

namespace EmergencyShift.Features.PauseMenu.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Pause menu audio")]
    public sealed class PauseMenuAudioConfig : ScriptableObject
    {
        [SerializeField] private AudioClip intro;
        [SerializeField] private AudioClip loop;
        [SerializeField, Range(0, 1)] private float volume = 0.4f;
        [SerializeField, Min(0)] private float fadeInSeconds = 1.5f;
        [SerializeField, Min(0)] private float fadeOutSeconds = 0.8f;
        [SerializeField, Min(0.01f)] private float introCrossfadeSeconds = 0.25f;

        public AudioClip Intro => intro;
        public AudioClip Loop => loop;
        public float Volume => volume;
        public float FadeInSeconds => fadeInSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
        public float IntroCrossfadeSeconds => introCrossfadeSeconds;

        public void Validate()
        {
            if (intro == null || loop == null || intro.samples <= 0 || loop.samples <= 0)
            {
                throw new InvalidOperationException("Pause music requires the authored intro and loop clips.");
            }

            if (!float.IsFinite(volume) || volume < 0 || volume > 1
                || !float.IsFinite(fadeInSeconds) || fadeInSeconds < 0
                || !float.IsFinite(fadeOutSeconds) || fadeOutSeconds < 0
                || !float.IsFinite(introCrossfadeSeconds) || introCrossfadeSeconds <= 0
                || introCrossfadeSeconds >= intro.length)
            {
                throw new InvalidOperationException("Pause music gains and transition durations are invalid.");
            }
        }
    }
}
