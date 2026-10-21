using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Scene-local layered atmosphere. Only perceived threats affect it; AI remains independent.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(80)]
    public sealed class LevelAudioDirector2D : MonoBehaviour
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

        [Inject]
        private void Construct(WorldMotionPause pause, PlayerLife2D scenePlayer, Flashlight2D sceneLight)
        {
            motionPause = pause;
            if (player == null)
            {
                player = scenePlayer;
            }

            if (flashlight == null)
            {
                flashlight = sceneLight;
            }
        }
        private float nextObservation;
        private float lastThreat = float.NegativeInfinity;
        private float lastCombat = float.NegativeInfinity;
        private float lastShot = float.NegativeInfinity;
        private float duckUntil = float.NegativeInfinity;
        private Vector3 gains;
        private bool started;
        private bool paused;
        private bool bound;
        private readonly Dictionary<AudioThreatEmitter2D, float> remembered = new Dictionary<AudioThreatEmitter2D, float>();
        private readonly List<AudioThreatEmitter2D> expired = new List<AudioThreatEmitter2D>();
        private double scheduledStart;
        public AudioMood Mood
        {
            get; private set;
        }
        public int PerceivedEnemies
        {
            get; private set;
        }
        public int EngagedEnemies
        {
            get; private set;
        }
        public Vector3 LayerGains => gains;
        public float Volume
        {
            get => volume; set => volume = Mathf.Clamp01(value);
        }
        public AudioMoodProfile Profile => profile;
        public bool IsDucking => Time.time < duckUntil;

        private void OnEnable()
        {
            if (profile != null)
            {
                profile.Validate();
            }

            lastThreat = lastCombat = lastShot = duckUntil = float.NegativeInfinity;
            nextObservation = 0;
            Mood = AudioMood.Explore;
            remembered.Clear();
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

        private void OnShot()
        {
            lastShot = Time.time;
            // A lethal projectile can resolve before the next observation tick.
            if (EngagedEnemies > 0)
            {
                lastCombat = lastThreat = Time.time;
            }

            if (profile != null)
            {
                duckUntil = Time.time + profile.ShotDuckSeconds;
            }
        }

        private void OnHealthChanged(int health)
        {
            if (health <= 0)
            {
                return;
            }

            lastCombat = lastThreat = Time.time;
            duckUntil = Time.time + (profile != null ? profile.DamageDuckSeconds : AudioMoodProfile.DefaultDamageDuckSeconds);
        }

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

            if (Time.time >= nextObservation)
            {
                nextObservation = Time.time + Mathf.Max(.05f, profile.ObservationInterval);
                Observe();
            }

            bool alive = !player.IsDead;
            float threatGain = Mood != AudioMood.Explore ? profile.ThreatGain : 0;
            float combatGain = Mood == AudioMood.Combat ? profile.CombatGain : 0;
            Vector3 target = alive ? new Vector3(profile.ExplorationGain, threatGain, combatGain) : Vector3.zero;
            float duck = alive && Time.time < duckUntil ? profile.DuckGain : 1;
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

        private void Observe()
        {
            PerceivedEnemies = EngagedEnemies = 0;
            if (player == null || player.IsDead)
            {
                Mood = AudioMood.Explore;
                return;
            }

            int close = 0;
            bool attack = false;
            expired.Clear();
            foreach (var pair in remembered)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled || !pair.Key.IsAlive || pair.Value < Time.time)
                {
                    expired.Add(pair.Key);
                }
            }

            foreach (var source in expired)
            {
                remembered.Remove(source);
            }

            float maxSquared = profile.AwarenessDistance * profile.AwarenessDistance;
            foreach (var source in AudioThreatEmitter2D.Active)
            {
                if (source == null || !source.IsAlive || source.gameObject.scene != gameObject.scene || (source.Target != null && source.Target != player.transform))
                {
                    continue;
                }

                float distanceSquared = ((Vector2)source.transform.position - (Vector2)player.transform.position).sqrMagnitude;
                if (distanceSquared > maxSquared)
                {
                    continue;
                }

                bool seen = IsVisible(source);
                bool heard = Time.time <= source.HeardUntil;
                if (seen || heard)
                {
                    remembered[source] = Time.time + profile.PerceptionMemorySeconds;
                }

                if (!seen && !heard && !remembered.ContainsKey(source))
                {
                    continue;
                }

                PerceivedEnemies++;
                if (source.IsEngaged)
                {
                    EngagedEnemies++;
                    if (distanceSquared <= profile.CloseCombatDistance * profile.CloseCombatDistance)
                    {
                        close++;
                    }
                }

                attack |= source.IsAttacking;
                if (Time.time <= source.UrgentUntil)
                {
                    duckUntil = Mathf.Max(duckUntil, Time.time + profile.CueDuckSeconds);
                }
            }

            if (PerceivedEnemies > 0)
            {
                lastThreat = Time.time;
            }

            // Pressure starts on approach, not only after the bite or the player's first shot.
            if (attack || close > 0 || EngagedEnemies >= profile.CrowdEnemiesForCombat || (EngagedEnemies > 0 && Time.time - lastShot < profile.CombatAfterShotSeconds))
            {
                lastCombat = Time.time;
            }

            Mood = SelectMood();
        }

        private AudioMood SelectMood()
        {
            if (Time.time - lastCombat < profile.CombatHoldSeconds)
            {
                return AudioMood.Combat;
            }

            if (Time.time - lastThreat < profile.ThreatHoldSeconds)
            {
                return AudioMood.Threat;
            }

            return AudioMood.Explore;
        }

        private bool IsVisible(AudioThreatEmitter2D source)
        {
            Vector2 point = source.transform.position;
            if (flashlight != null && flashlight.WorldCamera != null)
            {
                Vector3 view = flashlight.WorldCamera.WorldToViewportPoint(source.transform.position);
                if (view.z <= 0 || view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1)
                {
                    return false;
                }
            }

            if (flashlight != null && flashlight.Illuminates(point, source.Obstacles))
            {
                return true;
            }

            // A lamp can reveal a creature, but neither its light nor the music can see through the player's wall.
            if (Physics2D.Linecast(player.transform.position, point, source.Obstacles).collider != null)
            {
                return false;
            }

            foreach (var lamp in LocalLamp2D.Active)
            {
                if (lamp != null && lamp.gameObject.scene == gameObject.scene && lamp.Illuminates(point, source.Obstacles))
                {
                    return true;
                }
            }

            return false;
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
            remembered.Clear();
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
