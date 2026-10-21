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
    public sealed class LevelAudioDirector2D : MonoBehaviour, IAudioMoodView
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
        private readonly List<AudioThreatObservation> observations = new List<AudioThreatObservation>();

        private Vector3 gains;
        private bool started;
        private bool paused;
        private bool bound;
        private double scheduledStart;
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
            set => volume = Mathf.Clamp01(value);
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
        }

        void IAudioMoodView.ApplyMood(AudioMood value, int perceived, int engaged)
        {
            Mood = value;
            PerceivedEnemies = perceived;
            EngagedEnemies = engaged;
        }

        private void OnShot() => mood?.Shot();
        private void OnHealthChanged(int health) => mood?.Damage(health);

        private void Update()
        {
            if (profile == null || explorationSource == null || threatSource == null || combatSource == null)
            {
                return;
            }

            bool wantPause = AudioListener.pause || (Time.timeScale <= 0 && !motionPause.PreservesAudioPlayback);
            if (wantPause)
            {
                if (!paused && started)
                {
                    if (AudioSettings.dspTime < scheduledStart)
                    {
                        StopLayers();
                    }
                    else
                    {
                        explorationSource.Pause();
                        threatSource.Pause();
                        combatSource.Pause();
                    }
                }

                paused = true;
                return;
            }

            if (paused)
            {
                if (started)
                {
                    explorationSource.UnPause();
                    threatSource.UnPause();
                    combatSource.UnPause();
                }

                paused = false;
            }

            // Never reveal sources or play a new scene's bed before its explicit listener is available.
            if (!bound)
            {
                BindListener();
            }

            if (player == null)
            {
                if (started)
                {
                    StopLayers();
                }

                bound = false;
                return;
            }

            if (!started)
            {
                StartLayers();
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

            bool alive = !player.IsDead;
            float threatGain = Mood != AudioMood.Explore ? profile.ThreatGain : 0;
            float combatGain = Mood == AudioMood.Combat ? profile.CombatGain : 0;
            Vector3 target = alive ? new Vector3(profile.ExplorationGain, threatGain, combatGain) : Vector3.zero;
            float duck = alive && IsDucking ? profile.DuckGain : 1;
            float health = alive && player.Health < player.MaximumHealth ? profile.LowHealthGain : 1;
            target *= profile.MasterVolume * volume * duck * health;
            float seconds = alive ? profile.FallSeconds : profile.DeathFadeSeconds;
            for (int i = 0; i < 3; i++)
            {
                float tau = target[i] > gains[i] ? profile.RiseSeconds : seconds;
                // Urgent cues duck the bed promptly; recovery remains smooth.
                if (alive && duck < 1 && target[i] < gains[i])
                {
                    tau = profile.DuckResponseSeconds;
                }

                gains[i] = Mathf.Lerp(gains[i], target[i], 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(.02f, tau)));
            }

            explorationSource.volume = gains.x;
            threatSource.volume = gains.y;
            combatSource.volume = gains.z;
        }

        private void StartLayers()
        {
            scheduledStart = AudioSettings.dspTime + profile.ScheduleLeadSeconds;
            Schedule(explorationSource, profile.Exploration, scheduledStart, profile.LayerPriority);
            Schedule(threatSource, profile.Threat, scheduledStart, profile.LayerPriority);
            Schedule(combatSource, profile.Combat, scheduledStart, profile.LayerPriority);
            started = true;
        }

        private static void Schedule(AudioSource source, AudioClip clip, double at, int priority)
        {
            source.Stop();
            source.clip = clip;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.pitch = 1;
            // Keep three bed voices ahead of idle/footstep voices under the 32-real-voice budget.
            // Attack, wound and weapon cues still have higher priority.
            source.loop = true;
            source.priority = priority;
            source.volume = 0;
            if (clip != null)
            {
                source.PlayScheduled(at);
            }
        }

        private void StopLayers()
        {
            if (explorationSource != null)
            {
                explorationSource.Stop();
            }

            if (threatSource != null)
            {
                threatSource.Stop();
            }

            if (combatSource != null)
            {
                combatSource.Stop();
            }

            started = false;
        }

        private void OnAudioConfigurationChanged(bool deviceChanged)
        {
            StopLayers();
            gains = Vector3.zero;
        }

        private void OnDisable()
        {
            StopLayers();
            gains = Vector3.zero;
            paused = false;
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
