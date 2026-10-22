using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared authored audio data: one asset instead of 49 references on every creature.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Stalker audio set")]
    public sealed class StalkerAudioSet : ScriptableObject
    {

        [Header("Shared playback tuning")]
        [SerializeField]
        private float masterVolume = 1;
        public float MasterVolume => masterVolume;
        [SerializeField]
        private float movementVolume = .9f;
        public float MovementVolume => movementVolume;
        [SerializeField]
        private float creatureVolume = 1;
        public float CreatureVolume => creatureVolume;
        [SerializeField]
        private float distancePerContact = .22f;
        public float DistancePerContact => distancePerContact;
        [SerializeField]
        private float minimumContactInterval = .075f;
        public float MinimumContactInterval => minimumContactInterval;
        [SerializeField]
        private float frictionChance = .16f;
        public float FrictionChance => frictionChance;
        [SerializeField]
        private float contactPitch = 1;
        public float ContactPitch => contactPitch;
        [SerializeField]
        private Vector2 idlePause = new Vector2(3, 7);
        public Vector2 IdlePause => idlePause;
        [SerializeField]
        private Vector2 cowerPause = new Vector2(1.3f, 2.8f);
        public Vector2 CowerPause => cowerPause;
        [SerializeField]
        private Vector2 retreatPainPause = new Vector2(.32f, .55f);
        public Vector2 RetreatPainPause => retreatPainPause;
        [SerializeField]
        private float fullVolumeDistance = 2.5f;
        public float FullVolumeDistance => fullVolumeDistance;
        [SerializeField]
        private float hearingDistance = 11;
        public float HearingDistance => hearingDistance;
        [SerializeField]
        private float panDistance = 6;
        public float PanDistance => panDistance;
        [SerializeField]
        private float maximumPan = .75f;
        public float MaximumPan => maximumPan;
        [SerializeField]
        private float blockedVolume = .45f;
        public float BlockedVolume => blockedVolume;
        [SerializeField]
        private float blockedCutoff = 1300;
        public float BlockedCutoff => blockedCutoff;
        [SerializeField]
        private float obstructionInterval = .15f;
        public float ObstructionInterval => obstructionInterval;
        [SerializeField]
        private float minimumHitCueInterval = .05f;
        public float MinimumHitCueInterval => minimumHitCueInterval;
        public enum Cue
        {
            Step,
            Scrape,
            Idle,
            Notice,
            Windup,
            Lunge,
            Bite,
            LightFear,
            RetreatPain,
            Cower,
            Hit,
            Death,
            Debris,
            EyeBurn
        }

        [SerializeField]
        private AudioClip[] step;
        [SerializeField]
        private AudioClip[] scrape;
        [SerializeField]
        private AudioClip[] idle;
        [SerializeField]
        private AudioClip[] notice;
        [SerializeField]
        private AudioClip[] windup;
        [SerializeField]
        private AudioClip[] lunge;
        [SerializeField]
        private AudioClip[] bite;
        [SerializeField]
        private AudioClip[] lightFear;
        [SerializeField]
        private AudioClip[] retreatPain;
        [SerializeField]
        private AudioClip[] cower;
        [SerializeField]
        private AudioClip[] hit;
        [SerializeField, Tooltip("Legacy fallback when the shared death bank is not assigned.")]
        private AudioClip[] death;
        [SerializeField]
        private EnemyDeathAudioSet deathSounds;
        [SerializeField]
        private AudioClip[] debris;
        [SerializeField]
        private AudioClip[] eyeBurn;
        [SerializeField, Range(0, 1)]
        private float eyeBurnGain = .8f;
        [Header("Shared simultaneous voice limits")]
        [SerializeField, Min(1)]
        private int movementVoices = 8;
        [SerializeField, Min(1)]
        private int idleVoices = 2;
        [SerializeField, Min(1)]
        private int deathVoices = 3;

        [SerializeField] private float scrapeGain = .25f;
        public float ScrapeGain => scrapeGain;
        [SerializeField] private float retreatPainGain = .85f;
        public float RetreatPainGain => retreatPainGain;
        [SerializeField] private float cowerGain = .9f;
        public float CowerGain => cowerGain;
        [SerializeField] private float lungeGain = .9f;
        public float LungeGain => lungeGain;
        [SerializeField] private float debrisGain = .6f;
        public float DebrisGain => debrisGain;
        [SerializeField] private float retreatInitialPause = .12f;
        public float RetreatInitialPause => retreatInitialPause;
        [SerializeField] private float cowerInitialPause = .3f;
        public float CowerInitialPause => cowerInitialPause;
        [SerializeField] private float debrisInterval = .09f;
        public float DebrisInterval => debrisInterval;
        [SerializeField] private float obstructionResponse = 12;
        public float ObstructionResponse => obstructionResponse;
        [SerializeField] private float signalHeartbeat = .15f;
        public float SignalHeartbeat => signalHeartbeat;
        [SerializeField] private float minimumSignalGain = .02f;
        public float MinimumSignalGain => minimumSignalGain;
        [SerializeField] private float minimumSignalSeconds = .25f;
        public float MinimumSignalSeconds => minimumSignalSeconds;
        [SerializeField] private float maximumSignalSeconds = .5f;
        public float MaximumSignalSeconds => maximumSignalSeconds;

        public float CueGain(Cue cue)
        {
            switch (cue)
            {
                case Cue.Scrape: return scrapeGain;
                case Cue.RetreatPain: return retreatPainGain;
                case Cue.Cower: return cowerGain;
                case Cue.Lunge: return lungeGain;
                case Cue.Debris: return debrisGain;
                default: return 1;
            }
        }

        public EnemyDeathAudioSet DeathSounds => deathSounds;
        public float EyeBurnGain => eyeBurnGain;
        public int MovementVoices => movementVoices;
        public int IdleVoices => idleVoices;
        public int DeathVoices => deathVoices;

        public AudioClipBank Clips(Cue cue)
        {
            Validate();
            switch (cue)
            {
                case Cue.Step:
                    return step;
                case Cue.Scrape:
                    return scrape;
                case Cue.Idle:
                    return idle;
                case Cue.Notice:
                    return notice;
                case Cue.Windup:
                    return windup;
                case Cue.Lunge:
                    return lunge;
                case Cue.Bite:
                    return bite;
                case Cue.LightFear:
                    return lightFear;
                case Cue.RetreatPain:
                    return retreatPain;
                case Cue.Cower:
                    return cower;
                case Cue.Hit:
                    return hit;
                case Cue.Death:
                    return death;
                case Cue.Debris:
                    return debris;
                case Cue.EyeBurn:
                    return eyeBurn;
                default:
                    return default;
            }
        }

        public void Validate()
        {
            if (!AudioProfileValidation.Unit(scrapeGain) ||
                !AudioProfileValidation.Unit(retreatPainGain) ||
                !AudioProfileValidation.Unit(cowerGain) ||
                !AudioProfileValidation.Unit(lungeGain) ||
                !AudioProfileValidation.Unit(debrisGain) ||
                !AudioProfileValidation.Positive(retreatInitialPause) ||
                !AudioProfileValidation.Positive(cowerInitialPause) ||
                !AudioProfileValidation.Positive(debrisInterval) ||
                !AudioProfileValidation.Positive(obstructionResponse) ||
                !AudioProfileValidation.Positive(signalHeartbeat) ||
                !AudioProfileValidation.Unit(minimumSignalGain) ||
                !AudioProfileValidation.Positive(minimumSignalSeconds) ||
                !AudioProfileValidation.Positive(maximumSignalSeconds) || minimumSignalSeconds > maximumSignalSeconds)
            {
                throw new InvalidOperationException("Invalid native audio envelope: " + name);
            }

            if (!AudioProfileValidation.Unit(masterVolume) ||
                !AudioProfileValidation.Unit(movementVolume) ||
                !AudioProfileValidation.Unit(creatureVolume) ||
                !AudioProfileValidation.Positive(distancePerContact) ||
                !AudioProfileValidation.Positive(minimumContactInterval) ||
                !AudioProfileValidation.Unit(frictionChance) ||
                !AudioProfileValidation.Positive(contactPitch) ||
                !AudioProfileValidation.Interval(idlePause) ||
                !AudioProfileValidation.Interval(cowerPause) ||
                !AudioProfileValidation.Interval(retreatPainPause) ||
                !AudioProfileValidation.AtLeast(fullVolumeDistance, 0) ||
                !AudioProfileValidation.Positive(hearingDistance) ||
                !AudioProfileValidation.Positive(panDistance) ||
                !AudioProfileValidation.Unit(maximumPan) ||
                !AudioProfileValidation.Unit(blockedVolume) ||
                !(AudioProfileValidation.AtLeast(blockedCutoff, 200) && blockedCutoff <= 22000) ||
                !AudioProfileValidation.Positive(obstructionInterval) ||
                !AudioProfileValidation.AtLeast(minimumHitCueInterval, 0) || fullVolumeDistance >= hearingDistance)
            {
                throw new System.InvalidOperationException("Invalid playback tuning: " + name);
            }

            if (!AudioProfileValidation.Unit(eyeBurnGain) || movementVoices < 1 || idleVoices < 1 || deathVoices < 1)
            {
                throw new InvalidOperationException("Invalid Stalker audio set: " + name);
            }
        }
    }
}
