using EmergencyShift.Features.PauseMenu.Config;
using UnityEngine;

namespace EmergencyShift.Features.PauseMenu.View
{
    /// <summary>Unscaled menu fades; prepared PCM loop plays natively without frame-based seeking.</summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuMusic : MonoBehaviour
    {
        [SerializeField] private PauseMenuAudioConfig config;
        [SerializeField] private AudioSource introSource;
        [SerializeField] private AudioSource loopSource;
        private float gain;
        private float transition;
        private bool menuOpen;
        private bool playing;
        private bool loopStarted;
        private double startedAt;

        public PauseMenuAudioConfig Config => config;
        public bool IsPlaying => playing;
        public float Gain => gain;

        private void Awake()
        {
            if (config == null || introSource == null || loopSource == null || introSource == loopSource)
            {
                throw new System.InvalidOperationException("Pause music requires a config and two distinct sources.");
            }

            config.Validate();
            ConfigureSource(introSource, config.Intro, false);
            ConfigureSource(loopSource, config.Loop, true);
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            float seconds = menuOpen ? config.FadeInSeconds : config.FadeOutSeconds;
            float target = menuOpen ? 1 : 0;
            gain = seconds <= 0 ? target : Mathf.MoveTowards(gain, target, Time.unscaledDeltaTime / seconds);
            AdvanceIntro();
            float level = config.Volume * Mathf.SmoothStep(0, 1, gain);
            introSource.volume = level * (1 - transition);
            loopSource.volume = level * transition;
            if (!menuOpen && gain <= 0)
            {
                StopImmediate();
            }
        }

        public void SetMenuOpen(bool open)
        {
            menuOpen = open;
            if (open && !playing)
            {
                gain = 0;
                transition = 0;
                loopStarted = false;
                introSource.volume = 0;
                loopSource.volume = 0;
                startedAt = Time.unscaledTimeAsDouble;
                playing = true;
                introSource.Play();
            }
        }

        public void StopImmediate()
        {
            menuOpen = false;
            playing = false;
            gain = 0;
            transition = 0;
            loopStarted = false;
            if (introSource != null)
            {
                introSource.Stop();
                introSource.volume = 0;
            }

            if (loopSource != null)
            {
                loopSource.Stop();
                loopSource.volume = 0;
            }
        }

        private void OnDisable() => StopImmediate();

        private void AdvanceIntro()
        {
            float remaining = (config.Intro.samples - introSource.timeSamples) / (float)config.Intro.frequency;
            bool ended = Time.unscaledTimeAsDouble - startedAt >= config.Intro.length;
            if (!loopStarted && (remaining <= config.IntroCrossfadeSeconds || ended))
            {
                // Intro's final overlap is the same PCM as the loop's beginning.
                // Compensate for the frame that crossed the overlap boundary.
                float intoOverlap = ended
                    ? config.IntroCrossfadeSeconds
                    : Mathf.Max(0, config.IntroCrossfadeSeconds - remaining);
                loopSource.timeSamples = Mathf.RoundToInt(intoOverlap * config.Loop.frequency) % config.Loop.samples;
                loopSource.Play();
                loopStarted = true;
            }

            if (loopStarted)
            {
                transition = ended ? 1 : Mathf.Clamp01(1 - remaining / config.IntroCrossfadeSeconds);
                if (transition >= 1 && introSource.isPlaying)
                {
                    introSource.Stop();
                }
            }
        }

        private static void ConfigureSource(AudioSource source, AudioClip clip, bool loop)
        {
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = loop;
            source.spatialBlend = 0;
            source.ignoreListenerPause = true;
            source.dopplerLevel = 0;
            source.volume = 0;
            source.priority = 16;
        }
    }
}
