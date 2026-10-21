using System;
using System.Collections.Generic;
using EmergencyShift.Features.AudioMood;
using EmergencyShift.Features.AudioMood.Logic;
using EmergencyShift.Features.AudioMood.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Scene-local layered atmosphere. Only perceived threats affect it; AI remains independent.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(80)]
    public sealed class LevelAudioDirector2D : MonoBehaviour, IAudioMoodView, IAudioLayerMixView
    {
        [SerializeField]
        private AudioMoodProfile profile;
        [SerializeField]
        private PlayerLife2D player;
        [SerializeField]
        private Flashlight2D flashlight;
        [SerializeField]
        private AudioSource explorationSource;
        [SerializeField]
        private AudioSource threatSource;
        [SerializeField]
        private AudioSource combatSource;
        [SerializeField, Range(0, 1)]
        private float volume = 1;
        private PlayerWeapon2D weapon;
        private WorldMotionPause motionPause;
        private AudioThreatQuery2D threatQuery;
        private AudioMoodControllerFactory moodFactory;
        private AudioMoodController mood;
        private AudioLayerMixController mix;
        private AudioLayerPlayback playback;
        private readonly List<AudioThreatObservation> observations = new List<AudioThreatObservation>();

        private Vector3 gains;
        private bool bound;
        public AudioMood Mood
        {
            get;
            private set;
        }
        public int PerceivedEnemies
        {
            get;
            private set;
        }
        public int EngagedEnemies
        {
            get;
            private set;
        }
        public Vector3 LayerGains => gains;
        public float Volume
        {
            get => volume;
            set
            {
                if (!float.IsFinite(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                volume = Mathf.Clamp01(value);
            }
        }
        public AudioMoodProfile Profile => profile;
        public int RegisteredThreatCount => threatQuery.RegisteredCount;
        public bool IsDucking => mood != null && mood.IsDucking;

        [Inject]
        private void Construct(WorldMotionPause pause, PlayerLife2D scenePlayer, Flashlight2D sceneLight,
            AudioMoodControllerFactory factory, AudioThreatQuery2D query)
        {
            motionPause = pause;
            moodFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            threatQuery = query ?? throw new ArgumentNullException(nameof(query));
            if (player == null)
            {
                player = scenePlayer;
            }

            if (flashlight == null)
            {
                flashlight = sceneLight;
            }

            ConfigureMood();
        }

        private void OnEnable()
        {
            playback = new AudioLayerPlayback(explorationSource, threatSource, combatSource);
            if (profile != null)
            {
                profile.Validate();
            }

            ConfigureMood();
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        private void Start()
        {
            BindListener();
        }

        private void BindListener()
        {
            if (bound)
            {
                return;
            }

            if (player != null)
            {
                weapon = player.GetComponent<PlayerWeapon2D>();
                player.HealthChanged += OnHealthChanged;
                if (weapon != null)
                {
                    weapon.ShotFired += OnShot;
                }

                bound = true;
            }
        }

        private void ConfigureMood()
        {
            if (profile == null || moodFactory == null)
            {
                return;
            }

            mood = moodFactory.Create(profile.ToMoodSettings(), this);
            mood.Reset();
            mix = moodFactory.CreateMix(profile.ToMixSettings(), this);
            mix.Reset();
        }

        void IAudioMoodView.ApplyMood(AudioMood value, int perceived, int engaged)
        {
            Mood = value;
            PerceivedEnemies = perceived;
            EngagedEnemies = engaged;
        }

        void IAudioLayerMixView.ApplyGains(Vector3 value)
        {
            gains = value;
            playback?.ApplyGains(value);
        }

        private void OnShot() => mood?.Shot();
        private void OnHealthChanged(int health) => mood?.Damage(health);

        private void Update()
        {
            if (profile == null || !playback.IsConfigured)
            {
                return;
            }

            bool wantPause = AudioListener.pause || (Time.timeScale <= 0 && !motionPause.PreservesAudioPlayback);
            if (playback.UpdatePause(wantPause))
            {
                return;
            }

            // Never reveal sources or play a new scene's bed before its explicit listener is available.
            if (!bound)
            {
                BindListener();
            }

            if (player == null)
            {
                if (playback.Started)
                {
                    playback.Stop();
                }

                bound = false;
                return;
            }

            if (!playback.Started)
            {
                playback.Start(profile);
            }

            if (mood.ObservationDue)
            {
                observations.Clear();
                if (!player.IsDead)
                {
                    threatQuery.Collect(gameObject.scene, player.transform, flashlight, mood.AwarenessDistance, observations);
                }

                mood.Observe(observations, !player.IsDead);
            }

            mix.Step(Time.deltaTime, Mood, !player.IsDead, player.Health < player.MaximumHealth, IsDucking, volume);
        }

        private void OnAudioConfigurationChanged(bool deviceChanged)
        {
            playback?.Stop();
            mix?.Reset();
        }

        private void OnDisable()
        {
            playback?.Dispose();
            mix?.Reset();
            observations.Clear();
            mood?.Reset();
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
            if (player != null)
            {
                player.HealthChanged -= OnHealthChanged;
            }

            if (weapon != null)
            {
                weapon.ShotFired -= OnShot;
            }

            bound = false;
        }
    }
}
