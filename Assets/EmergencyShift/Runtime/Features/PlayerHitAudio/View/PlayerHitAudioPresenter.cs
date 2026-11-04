using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.PlayerHitAudio.Config;
using EmergencyShift.Features.WorldAudio.View;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace EmergencyShift.Features.PlayerHitAudio.View
{
    /// <summary>Player-owned cosmetic response to accepted damage; never requests health changes.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHitAudioPresenter : MonoBehaviour, IDisposable
    {
        private const int VoiceBudget = 2;
        private readonly AudioSource[] voices = new AudioSource[VoiceBudget];
        private PlayerLife2D player;
        private PlayerHitAudioProfile profile;
        private WorldAudioBus bus;
        private IRandomSource random;
        private GameObject voiceRoot;
        private int currentVoice = -1;
        private int previousVoice = -1;
        private int lastChoice = -1;
        private double fadeStarted;
        private float previousGain;
        private float targetGain;
        private bool fading;
        private bool initialized;
        private bool subscribed;
        private bool disposed;

        public int PlayedCount { get; private set; }
        public AudioClip LastClip { get; private set; }
        public int ActiveVoiceCount
        {
            get
            {
                int count = 0;
                foreach (AudioSource voice in voices)
                {
                    if (voice != null && voice.isPlaying)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        [Inject]
        private void Construct(PlayerLife2D player, PlayerHitAudioProfile profile, WorldAudioBus bus,
            [Inject(Id = RandomSourceIds.Cosmetic)] RandomSourceFactory randoms)
        {
            this.player = player;
            this.profile = profile;
            this.bus = bus;
            random = randoms.Create();
        }

        private void Start()
        {
            if (disposed)
            {
                return;
            }
            profile.Validate();
            voiceRoot = new GameObject("Player hit voices (runtime)");
            SceneManager.MoveGameObjectToScene(voiceRoot, gameObject.scene);
            voiceRoot.transform.SetParent(transform, false);
            for (int index = 0; index < voices.Length; index++)
            {
                var voice = voiceRoot.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0;
                voice.dopplerLevel = 0;
                voice.priority = 32;
                voice.ignoreListenerPause = false;
                voice.volume = 0;
                bus.Route(voice);
                voices[index] = voice;
            }
            initialized = true;
            Attach();
        }

        private void OnEnable() => Attach();
        private void OnDisable() => Detach();

        private void Update()
        {
            if (!fading || AudioListener.pause)
            {
                return;
            }
            float progress = Mathf.Clamp01((float)((AudioSettings.dspTime - fadeStarted) / profile.OverlapFadeSeconds));
            float remaining = previousGain * (1 - progress);
            voices[previousVoice].volume = remaining;
            voices[currentVoice].volume = targetGain - remaining;
            if (progress >= 1)
            {
                voices[previousVoice].Stop();
                previousVoice = -1;
                fading = false;
            }
        }

        private void OnDestroy()
        {
            Dispose();
            if (voiceRoot != null)
            {
                Destroy(voiceRoot);
            }
        }

        public void Dispose()
        {
            disposed = true;
            Detach();
        }

        private void Attach()
        {
            if (!initialized || disposed || subscribed)
            {
                return;
            }
            player.Damaged += OnDamaged;
            player.HealthRestored += OnHealthRestored;
            subscribed = true;
        }

        private void Detach()
        {
            if (subscribed)
            {
                player.Damaged -= OnDamaged;
                player.HealthRestored -= OnHealthRestored;
                subscribed = false;
            }
            StopVoices();
        }

        private void OnDamaged(int amount, Vector2 direction)
        {
            if (amount <= 0 || disposed || !isActiveAndEnabled)
            {
                return;
            }
            int choice = random.NextInt(0, profile.Clips.Count);
            if (profile.Clips.Count > 1 && choice == lastChoice)
            {
                choice = (choice + random.NextInt(1, profile.Clips.Count)) % profile.Clips.Count;
            }

            int oldVoice = currentVoice;
            bool overlap = oldVoice >= 0 && voices[oldVoice].isPlaying;
            int nextVoice = overlap ? 1 - oldVoice : 0;
            if (fading)
            {
                voices[previousVoice].Stop();
                fading = false;
            }

            AudioSource next = voices[nextVoice];
            next.Stop();
            next.clip = profile.Clips[choice];
            next.pitch = 1 + (float)(random.NextUnit() * 2 - 1) * profile.PitchVariation;
            targetGain = profile.Volume;
            if (overlap)
            {
                previousVoice = oldVoice;
                previousGain = Mathf.Min(voices[oldVoice].volume, targetGain * .5f);
                voices[oldVoice].volume = previousGain;
                next.volume = targetGain - previousGain;
                fadeStarted = AudioSettings.dspTime;
                fading = true;
            }
            else
            {
                next.volume = targetGain;
                previousVoice = -1;
            }
            currentVoice = nextVoice;
            lastChoice = choice;
            LastClip = next.clip;
            next.Play();
            PlayedCount++;
        }

        private void OnHealthRestored() => StopVoices();

        private void StopVoices()
        {
            foreach (AudioSource voice in voices)
            {
                if (voice != null)
                {
                    voice.Stop();
                    voice.volume = 0;
                }
            }
            fading = false;
            currentVoice = -1;
            previousVoice = -1;
        }
    }
}
