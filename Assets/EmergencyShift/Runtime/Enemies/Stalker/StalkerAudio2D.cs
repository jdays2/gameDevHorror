using System.Collections.Generic;
using EmergencyShift.Core.Composition;
using UnityEngine;
using Zenject;
using Cue = EmergencyShift.StalkerAudioSet.Cue;

namespace EmergencyShift
{
    /// <summary>Two bounded voices per creature. AI events drive reactions; real travel drives feet.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ShadowStalker2D))]
    public sealed class StalkerAudio2D : MonoBehaviour
    {
        [SerializeField, Tooltip("Use all local values, including values equal to former defaults.")]
        private bool useAuthoredAudioTuning;
        private float MasterVolume => AudioProfileValidation.Resolve(masterVolume, 1, sounds != null ? sounds.MasterVolume : 1, useAuthoredAudioTuning);
        private float MovementVolume => AudioProfileValidation.Resolve(movementVolume, .9f, sounds != null ? sounds.MovementVolume : .9f, useAuthoredAudioTuning);
        private float CreatureVolume => AudioProfileValidation.Resolve(creatureVolume, 1, sounds != null ? sounds.CreatureVolume : 1, useAuthoredAudioTuning);
        private float DistancePerContact => AudioProfileValidation.Resolve(distancePerContact, .22f, sounds != null ? sounds.DistancePerContact : .22f, useAuthoredAudioTuning);
        private float MinimumContactInterval => AudioProfileValidation.Resolve(minimumContactInterval, .075f, sounds != null ? sounds.MinimumContactInterval : .075f, useAuthoredAudioTuning);
        private float FrictionChance => AudioProfileValidation.Resolve(frictionChance, .16f, sounds != null ? sounds.FrictionChance : .16f, useAuthoredAudioTuning);
        private float ContactPitch => AudioProfileValidation.Resolve(contactPitch, 1, sounds != null ? sounds.ContactPitch : 1, useAuthoredAudioTuning);
        private Vector2 IdlePause => AudioProfileValidation.Resolve(idlePause, new Vector2(3, 7), sounds != null ? sounds.IdlePause : new Vector2(3, 7), useAuthoredAudioTuning);
        private Vector2 CowerPause => AudioProfileValidation.Resolve(cowerPause, new Vector2(1.3f, 2.8f), sounds != null ? sounds.CowerPause : new Vector2(1.3f, 2.8f), useAuthoredAudioTuning);
        private Vector2 RetreatPainPause => AudioProfileValidation.Resolve(retreatPainPause, new Vector2(.32f, .55f), sounds != null ? sounds.RetreatPainPause : new Vector2(.32f, .55f), useAuthoredAudioTuning);
        private float FullVolumeDistance => AudioProfileValidation.Resolve(fullVolumeDistance, 2.5f, sounds != null ? sounds.FullVolumeDistance : 2.5f, useAuthoredAudioTuning);
        private float HearingDistance => AudioProfileValidation.Resolve(hearingDistance, 11, sounds != null ? sounds.HearingDistance : 11, useAuthoredAudioTuning);
        private float PanDistance => AudioProfileValidation.Resolve(panDistance, 6, sounds != null ? sounds.PanDistance : 6, useAuthoredAudioTuning);
        private float MaximumPan => AudioProfileValidation.Resolve(maximumPan, .75f, sounds != null ? sounds.MaximumPan : .75f, useAuthoredAudioTuning);
        private float BlockedVolume => AudioProfileValidation.Resolve(blockedVolume, .45f, sounds != null ? sounds.BlockedVolume : .45f, useAuthoredAudioTuning);
        private float BlockedCutoff => AudioProfileValidation.Resolve(blockedCutoff, 1300, sounds != null ? sounds.BlockedCutoff : 1300, useAuthoredAudioTuning);
        private float ObstructionInterval => AudioProfileValidation.Resolve(obstructionInterval, .15f, sounds != null ? sounds.ObstructionInterval : .15f, useAuthoredAudioTuning);
        private float MinimumHitCueInterval => AudioProfileValidation.Resolve(minimumHitCueInterval, .05f, sounds != null ? sounds.MinimumHitCueInterval : .05f, useAuthoredAudioTuning);
        [Header("Saved references — assigned in prefab")]
        [SerializeField]
        private StalkerAudioSet sounds;
        [SerializeField]
        private AudioSource movementSource;
        [SerializeField]
        private AudioSource voiceSource;
        [SerializeField]
        private AudioLowPassFilter movementFilter;
        [SerializeField]
        private AudioLowPassFilter voiceFilter;
        [Header("Levels")]
        [SerializeField, Range(0, 1)]
        private float masterVolume = 1;
        [SerializeField, Range(0, 1)]
        private float movementVolume = .9f;
        [SerializeField, Range(0, 1)]
        private float creatureVolume = 1;
        [Header("Feet — distance, not playback pitch")]
        [SerializeField, Min(.05f)]
        private float distancePerContact = .22f;
        [SerializeField, Min(.02f)]
        private float minimumContactInterval = .075f;
        [SerializeField, Range(0, .5f)]
        private float frictionChance = .16f;
        [SerializeField, Range(.9f, 1.1f)]
        private float contactPitch = 1;
        [Header("Sparse reactions — random intervals")]
        [SerializeField]
        private Vector2 idlePause = new Vector2(3, 7);
        [SerializeField]
        private Vector2 cowerPause = new Vector2(1.3f, 2.8f);
        [SerializeField]
        private Vector2 retreatPainPause = new Vector2(.32f, .55f);
        [Header("XY hearing relative to the player")]
        [SerializeField, Min(.1f)]
        private float fullVolumeDistance = 2.5f;
        [SerializeField, Min(1)]
        private float hearingDistance = 11;
        [SerializeField, Min(.5f)]
        private float panDistance = 6;
        [SerializeField, Range(0, 1)]
        private float maximumPan = .75f;
        [Header("High walls and closed door muffle, not silence")]
        [SerializeField, Range(.05f, 1)]
        private float blockedVolume = .45f;
        [SerializeField, Range(200, 22000)]
        private float blockedCutoff = 1300;
        [SerializeField, Min(.05f)]
        private float obstructionInterval = .15f;
        private SceneRegistry<StalkerAudio2D> registry;
        private EnemyDeathAudioVariants deathVariants;
        private readonly int[] lastVariant = new int[System.Enum.GetValues(typeof(Cue)).Length];
        private readonly int[] playCounts = new int[System.Enum.GetValues(typeof(Cue)).Length];
        private readonly List<GibMotion2D> fallingParts = new List<GibMotion2D>(8);
        private ShadowStalker2D stalker;
        private Rigidbody2D body;
        private GoreBurst2D gore;
        private Vector2 previousPosition;
        private Vector2 movementPosition;
        private float travelled;
        private float nextContact;
        private float nextIdle;
        private float nextCower;
        private float nextPain;
        private float nextObstruction;
        private float obstruction = 1;
        private float cutoff = 22000;
        private float voiceGain = 1;
        private float movementGain = 1;
        private float nextDebris;
        private int debrisCount;
        private bool deathHeard;
        private Cue voiceCue;
        private Cue movementCue;
        private System.Random random;
        public AudioSource VoiceSource => voiceSource;
        public AudioSource MovementSource => movementSource;
        public StalkerAudioSet Sounds => sounds;
        public float ObstructionGain => obstruction;

        /// <summary>Presentation signal after real playback gain is known; silent or muted cues do not reveal enemies.</summary>
        public event System.Action<float, float, bool> PerceptibleCue;
        private float nextPerceptionSignal;
        [SerializeField, Min(0)]
        private float minimumHitCueInterval = .05f;
        private float nextHitCue;
        public int PlaybackCount(Cue cue) => playCounts[(int)cue];
        [Inject]
        private void Construct(EnemyDeathAudioVariants variants, SceneRegistry<StalkerAudio2D> sceneRegistry)
        {
            deathVariants = variants;
            registry?.Remove(this);
            registry = sceneRegistry;
            if (isActiveAndEnabled)
            {
                registry.Register(this);
            }
        }

        private void ValidateResolvedTuning()
        {
            if (!AudioProfileValidation.Unit(MasterVolume) ||
                !AudioProfileValidation.Unit(MovementVolume) ||
                !AudioProfileValidation.Unit(CreatureVolume) ||
                !AudioProfileValidation.Positive(DistancePerContact) ||
                !AudioProfileValidation.Positive(MinimumContactInterval) ||
                !AudioProfileValidation.Unit(FrictionChance) ||
                !AudioProfileValidation.Positive(ContactPitch) ||
                !AudioProfileValidation.Interval(IdlePause) ||
                !AudioProfileValidation.Interval(CowerPause) ||
                !AudioProfileValidation.Interval(RetreatPainPause) ||
                !AudioProfileValidation.AtLeast(FullVolumeDistance, 0) ||
                !AudioProfileValidation.Positive(HearingDistance) ||
                !AudioProfileValidation.Positive(PanDistance) ||
                !AudioProfileValidation.Unit(MaximumPan) ||
                !AudioProfileValidation.Unit(BlockedVolume) ||
                (!AudioProfileValidation.AtLeast(BlockedCutoff, 200) || BlockedCutoff > 22000) ||
                !AudioProfileValidation.Positive(ObstructionInterval) ||
                !AudioProfileValidation.AtLeast(MinimumHitCueInterval, 0) ||
                FullVolumeDistance >= HearingDistance)
            {
                throw new System.InvalidOperationException("Invalid resolved audio tuning: " + name);
            }
        }

        private void Awake()
        {
            sounds?.Validate();
            ValidateResolvedTuning();
            stalker = GetComponent<ShadowStalker2D>();
            body = GetComponent<Rigidbody2D>();
            gore = GetComponent<GoreBurst2D>();
            random = new System.Random(System.Guid.NewGuid().GetHashCode());
            for (int i = 0; i < lastVariant.Length; i++)
            {
                lastVariant[i] = -1;
            }

            Prepare(movementSource);
            Prepare(voiceSource);
        }

        private static void Prepare(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.pitch = 1;
            source.Stop();
        }

        private void OnEnable()
        {
            registry?.Register(this);

            previousPosition = body.position;
            movementPosition = body.position;
            travelled = 0;
            nextIdle = Time.time + Pause(IdlePause);
            nextCower = Time.time + Pause(CowerPause);
            nextObstruction = 0;
            stalker.StateChanged += OnState;
            stalker.Damaged += OnHit;
            stalker.EyeBurnStarted += OnEyeBurn;
            stalker.AttackLunged += OnLunge;
            stalker.AttackResolved += OnStrike;
            if (gore != null)
            {
                gore.FragmentLaunched += OnFragment;
            }
        }

        private void OnDisable()
        {
            registry?.Remove(this);
            if (stalker != null)
            {
                stalker.StateChanged -= OnState;
                stalker.Damaged -= OnHit;
                stalker.EyeBurnStarted -= OnEyeBurn;
                stalker.AttackLunged -= OnLunge;
                stalker.AttackResolved -= OnStrike;
            }

            if (gore != null)
            {
                gore.FragmentLaunched -= OnFragment;
            }

            foreach (var part in fallingParts)
            {
                if (part != null)
                {
                    part.FirstLanded -= OnLanding;
                }
            }

            fallingParts.Clear();
            if (movementSource != null)
            {
                movementSource.Stop();
            }

            if (voiceSource != null)
            {
                voiceSource.Stop();
            }
        }

        private float Pause(Vector2 range)
        {
            float minimum = Mathf.Max(.1f, range.x);
            return Mathf.Lerp(minimum, Mathf.Max(minimum, range.y), (float)random.NextDouble());
        }

        private bool Audible => stalker.Player != null && DistanceSquared < HearingDistance * HearingDistance;
        private float DistanceSquared => stalker.Player == null ? float.PositiveInfinity : ((Vector2)stalker.Player.position - (Vector2)transform.position).sqrMagnitude;

        private void Update()
        {
            if (sounds == null || movementSource == null || voiceSource == null)
            {
                return;
            }

            if (stalker.State == ShadowStalker2D.BehaviourState.Dead && stalker.DeathTime >= 2.5f && !movementSource.isPlaying && !voiceSource.isPlaying && fallingParts.Count == 0)
            {
                enabled = false;
                return;
            }

            if (!stalker.isActiveAndEnabled)
            {
                movementSource.Stop();
                voiceSource.Stop();
                travelled = 0;
                previousPosition = body.position;
                return;
            }

            UpdateMix();
            Vector2 position = body.position;
            float distance = Vector2.Distance(position, previousPosition);
            previousPosition = position;
            var state = stalker.State;
            bool walking = state == ShadowStalker2D.BehaviourState.Sneaking || state == ShadowStalker2D.BehaviourState.Patrolling || state == ShadowStalker2D.BehaviourState.Retreating;
            if (!walking || stalker.IsStaggered || distance >= 1.5f)
            {
                travelled = 0;
            }
            else if (distance > .0001f)
            {
                travelled += distance;
                if (travelled >= DistancePerContact && Time.time >= nextContact)
                {
                    travelled %= DistancePerContact;
                    nextContact = Time.time + MinimumContactInterval;
                    if (MovementBudgetAvailable())
                    {
                        Cue cue = random.NextDouble() < FrictionChance ? Cue.Scrape : Cue.Step;
                        PlayMovement(cue, 1, position);
                    }
                }
            }

            if (state == ShadowStalker2D.BehaviourState.Retreating && Time.time >= nextPain)
            {
                nextPain = Time.time + Pause(RetreatPainPause);
                PlayVoice(Cue.RetreatPain, 1, false);
            }
            else if (state == ShadowStalker2D.BehaviourState.Waiting && Time.time >= nextCower)
            {
                nextCower = Time.time + Pause(CowerPause);
                if (QuietBudgetAvailable())
                {
                    PlayVoice(Cue.Cower, 1, false);
                }
            }
            else if ((state == ShadowStalker2D.BehaviourState.Dormant || state == ShadowStalker2D.BehaviourState.Patrolling || state == ShadowStalker2D.BehaviourState.Sneaking) && Time.time >= nextIdle)
            {
                nextIdle = Time.time + Pause(IdlePause);
                if (!stalker.IsStaggered && QuietBudgetAvailable())
                {
                    PlayVoice(Cue.Idle, 1, false);
                }
            }
        }

        private void OnState(ShadowStalker2D.BehaviourState state)
        {
            travelled = 0;
            previousPosition = body.position;
            if (state != ShadowStalker2D.BehaviourState.Attacking)
            {
                if (voiceSource != null && voiceCue == Cue.Windup)
                {
                    voiceSource.Stop();
                }

                if (movementSource != null && movementCue == Cue.Lunge)
                {
                    movementSource.Stop();
                }
            }

            switch (state)
            {
                case ShadowStalker2D.BehaviourState.Attacking:
                    PlayVoice(Cue.Windup, 1, true);
                    break;
                case ShadowStalker2D.BehaviourState.Recoiling:
                    PlayVoice(Cue.LightFear, 1, true);
                    break;
                case ShadowStalker2D.BehaviourState.Retreating:
                    nextPain = Time.time + (sounds != null ? sounds.RetreatInitialPause : 0);
                    break;
                case ShadowStalker2D.BehaviourState.Waiting:
                    nextCower = Time.time + (sounds != null ? sounds.CowerInitialPause : 0);
                    break;
                case ShadowStalker2D.BehaviourState.Dead:
                    movementSource?.Stop();
                    debrisCount = 0;
                    nextDebris = Time.time;
                    deathHeard = DeathBudgetAvailable() && PlayVoice(Cue.Death, 1, true);
                    if (!deathHeard)
                    {
                        voiceSource?.Stop();
                    }

                    break;
                case ShadowStalker2D.BehaviourState.TargetDead:
                    movementSource?.Stop();
                    if (voiceSource != null && voiceCue != Cue.Bite)
                    {
                        voiceSource.Stop();
                    }

                    break;
            }
        }

        private void OnHit()
        {
            if (Time.time < nextHitCue)
            {
                return;
            }

            nextHitCue = Time.time + MinimumHitCueInterval;
            PlayVoice(Cue.Hit, 1, true);
        }

        private void OnEyeBurn()
        {
            if (sounds == null || !MovementBudgetAvailable())
            {
                return;
            }

            PlayMovement(Cue.EyeBurn, sounds.EyeBurnGain, transform.position);
            if (movementSource != null && movementCue == Cue.EyeBurn && movementSource.clip != null)
            {
                nextContact = Time.time + movementSource.clip.length;
            }
        }

        private void OnLunge()
        {
            PlayMovement(Cue.Lunge, 1, transform.position);
        }

        private void OnStrike(bool landed)
        {
            if (landed)
            {
                PlayVoice(Cue.Bite, 1, true);
            }
        }

        private AudioClip Choose(Cue cue)
        {
            if (cue == Cue.Death && sounds != null && sounds.DeathSounds != null)
            {
                return deathVariants.Choose(sounds.DeathSounds);
            }

            var clips = sounds != null ? sounds.Clips(cue) : default(AudioClipBank);
            if (clips.Count == 0)
            {
                return null;
            }

            int index = random.Next(clips.Count);
            if (clips.Count > 1 && index == lastVariant[(int)cue])
            {
                index = (index + random.Next(1, clips.Count)) % clips.Count;
            }

            lastVariant[(int)cue] = index;
            return clips[index];
        }

        private bool PlayVoice(Cue cue, float gain, bool interrupt)
        {
            if (voiceSource == null || !Audible || (!interrupt && voiceSource.isPlaying))
            {
                return false;
            }

            AudioClip clip = Choose(cue);
            if (clip == null)
            {
                return false;
            }

            voiceCue = cue;
            voiceGain = gain * sounds.CueGain(cue);
            if (cue == Cue.Death && sounds.DeathSounds != null)
            {
                voiceGain *= sounds.DeathSounds.Volume;
            }
            voiceSource.clip = clip;
            voiceSource.pitch = 1;
            voiceSource.priority = VoicePriority(cue);
            UpdateMix();
            voiceSource.Play();
            playCounts[(int)cue]++;
            SignalPerception(voiceSource, cue);
            return true;
        }

        private void PlayMovement(Cue cue, float gain, Vector2 position)
        {
            if (movementSource == null || !Audible)
            {
                return;
            }

            AudioClip clip = Choose(cue);
            if (clip == null)
            {
                return;
            }

            movementCue = cue;
            movementGain = gain * sounds.CueGain(cue);
            movementPosition = position;
            movementSource.clip = clip;
            movementSource.pitch = cue == Cue.Step || cue == Cue.Scrape ? ContactPitch : 1;
            movementSource.priority = cue == Cue.Lunge || cue == Cue.EyeBurn ? 80 : 140;
            UpdateMix();
            movementSource.Play();
            playCounts[(int)cue]++;
            SignalPerception(movementSource, cue);
        }

        private bool MovementBudgetAvailable()
        {
            if (sounds == null || !Audible)
            {
                return false;
            }

            int nearer = 0;
            int self = registry.IndexOf(this);
            float distance = DistanceSquared;
            for (int i = 0; i < registry.Members.Count; i++)
            {
                StalkerAudio2D other = registry.Members[i];
                bool closer = other.DistanceSquared < distance - .0001f || Mathf.Abs(other.DistanceSquared - distance) <= .0001f && i < self;
                if (other != this && other.stalker != null && other.stalker.State != ShadowStalker2D.BehaviourState.Dead && other.stalker.MovementSpeed > .05f && closer)
                {
                    nearer++;
                }
            }

            return nearer < sounds.MovementVoices;
        }

        private bool QuietBudgetAvailable()
        {
            if (sounds == null || !Audible)
            {
                return false;
            }

            int playing = 0;
            foreach (StalkerAudio2D other in registry.Members)
            {
                if (other != this && other.voiceSource != null && other.voiceSource.isPlaying && (other.voiceCue == Cue.Idle || other.voiceCue == Cue.Cower))
                {
                    playing++;
                }
            }

            return playing < sounds.IdleVoices;
        }

        private bool DeathBudgetAvailable()
        {
            if (sounds == null || !Audible)
            {
                return false;
            }

            int count = 0;
            StalkerAudio2D furthest = null;
            float distance = DistanceSquared;
            foreach (StalkerAudio2D other in registry.Members)
            {
                if (other == this || other.voiceSource == null || !other.voiceSource.isPlaying || other.voiceCue != Cue.Death)
                {
                    continue;
                }

                count++;
                if (other.DistanceSquared > distance)
                {
                    distance = other.DistanceSquared;
                    furthest = other;
                }
            }

            if (count < sounds.DeathVoices)
            {
                return true;
            }

            if (furthest == null)
            {
                return false;
            }

            furthest.voiceSource.Stop();
            furthest.deathHeard = false;
            return true;
        }

        private void OnFragment(GibMotion2D part)
        {
            fallingParts.Add(part);
            part.FirstLanded += OnLanding;
        }

        private void OnLanding(GibMotion2D part)
        {
            part.FirstLanded -= OnLanding;
            fallingParts.Remove(part);
            if (!deathHeard || debrisCount >= 3 || Time.time < nextDebris)
            {
                return;
            }

            debrisCount++;
            nextDebris = Time.time + sounds.DebrisInterval;
            PlayMovement(Cue.Debris, 1, part.transform.position);
        }

        private void UpdateMix()
        {
            if (movementSource == null || voiceSource == null)
            {
                return;
            }

            if (stalker.Player == null)
            {
                movementSource.volume = 0;
                voiceSource.volume = 0;
                return;
            }

            if (Time.time >= nextObstruction)
            {
                nextObstruction = Time.time + ObstructionInterval;
                bool blocked = Physics2D.Linecast(transform.position, stalker.Player.position, stalker.BlockingGeometry).collider != null;
                targetObstruction = blocked ? BlockedVolume : 1;
                targetCutoff = blocked ? BlockedCutoff : 22000;
            }

            float smoothing = 1 - Mathf.Exp(-sounds.ObstructionResponse * Time.deltaTime);
            obstruction = Mathf.Lerp(obstruction, targetObstruction, smoothing);
            cutoff = Mathf.Lerp(cutoff, targetCutoff, smoothing);
            Vector2 listener = stalker.Player.position;
            MixSource(voiceSource, transform.position, listener, CreatureVolume * voiceGain);
            MixSource(movementSource, movementPosition, listener, MovementVolume * movementGain);
            if (voiceFilter != null)
            {
                voiceFilter.cutoffFrequency = cutoff;
            }

            if (movementFilter != null)
            {
                movementFilter.cutoffFrequency = cutoff;
            }

            if (Time.time >= nextPerceptionSignal)
            {
                nextPerceptionSignal = Time.time + sounds.SignalHeartbeat;
                SignalPerception(voiceSource, voiceCue);
                SignalPerception(movementSource, movementCue);
            }
        }

        private void SignalPerception(AudioSource source, Cue cue)
        {
            if (source == null || !source.isPlaying || source.isVirtual || source.mute || source.clip == null || source.volume < sounds.MinimumSignalGain || AudioListener.pause || AudioListener.volume <= .001f)
            {
                return;
            }

            float remaining = Mathf.Max(sounds.MinimumSignalSeconds, source.clip.length - source.time);
            bool urgent = cue == Cue.Windup || cue == Cue.LightFear || cue == Cue.Bite || cue == Cue.Hit;
            PerceptibleCue?.Invoke(Mathf.Min(remaining, sounds.MaximumSignalSeconds), source.volume, urgent);
        }

        private float targetObstruction = 1;
        private float targetCutoff = 22000;
        private void MixSource(AudioSource source, Vector2 position, Vector2 listener, float gain)
        {
            float distance = Vector2.Distance(position, listener);
            float falloff = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(FullVolumeDistance, Mathf.Max(FullVolumeDistance + .1f, HearingDistance), distance));
            source.volume = Mathf.Clamp01(MasterVolume * gain * falloff * obstruction);
            source.panStereo = Mathf.Clamp((position.x - listener.x) / PanDistance, -MaximumPan, MaximumPan);
        }

        private static int VoicePriority(Cue cue)
        {
            switch (cue)
            {
                case Cue.Windup:
                case Cue.LightFear:
                    return 24;
                case Cue.Death:
                    return 40;
                case Cue.Idle:
                case Cue.Cower:
                    return 180;
                default:
                    return 64;
            }
        }
    }
}
