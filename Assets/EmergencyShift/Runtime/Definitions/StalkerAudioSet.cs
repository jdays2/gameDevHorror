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
        [SerializeField]
        private float movementVolume = .9f;
        [SerializeField]
        private float creatureVolume = 1;
        [SerializeField]
        private float distancePerContact = .22f;
        [SerializeField]
        private float minimumContactInterval = .075f;
        [SerializeField]
        private float frictionChance = .16f;
        [SerializeField]
        private float contactPitch = 1;
        [SerializeField]
        private Vector2 idlePause = new Vector2(3, 7);
        [SerializeField]
        private Vector2 cowerPause = new Vector2(1.3f, 2.8f);
        [SerializeField]
        private Vector2 retreatPainPause = new Vector2(.32f, .55f);
        [SerializeField]
        private float fullVolumeDistance = 2.5f;
        [SerializeField]
        private float hearingDistance = 11;
        [SerializeField]
        private float panDistance = 6;
        [SerializeField]
        private float maximumPan = .75f;
        [SerializeField]
        private float blockedVolume = .45f;
        [SerializeField]
        private float blockedCutoff = 1300;
        [SerializeField]
        private float obstructionInterval = .15f;
        [SerializeField]
        private float minimumHitCueInterval = .05f;
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
        [SerializeField] private float retreatPainGain = .85f;
        [SerializeField] private float cowerGain = .9f;
        [SerializeField] private float lungeGain = .9f;
        [SerializeField] private float debrisGain = .6f;
        [SerializeField] private float retreatInitialPause = .12f;
        [SerializeField] private float cowerInitialPause = .3f;
        [SerializeField] private float debrisInterval = .09f;
        [SerializeField] private float obstructionResponse = 12;
        [SerializeField] private float signalHeartbeat = .15f;
        [SerializeField] private float minimumSignalGain = .02f;
        [SerializeField] private float minimumSignalSeconds = .25f;
        [SerializeField] private float maximumSignalSeconds = .5f;

        public float MasterVolume => masterVolume;
        public float MovementVolume => movementVolume;
        public float CreatureVolume => creatureVolume;
        public float DistancePerContact => distancePerContact;
        public float MinimumContactInterval => minimumContactInterval;
        public float FrictionChance => frictionChance;
        public float ContactPitch => contactPitch;
        public Vector2 IdlePause => idlePause;
        public Vector2 CowerPause => cowerPause;
        public Vector2 RetreatPainPause => retreatPainPause;
        public float FullVolumeDistance => fullVolumeDistance;
        public float HearingDistance => hearingDistance;
        public float PanDistance => panDistance;
        public float MaximumPan => maximumPan;
        public float BlockedVolume => blockedVolume;
        public float BlockedCutoff => blockedCutoff;
        public float ObstructionInterval => obstructionInterval;
        public float MinimumHitCueInterval => minimumHitCueInterval;
        public float ScrapeGain => scrapeGain;
        public float RetreatPainGain => retreatPainGain;
        public float CowerGain => cowerGain;
        public float LungeGain => lungeGain;
        public float DebrisGain => debrisGain;
        public float RetreatInitialPause => retreatInitialPause;
        public float CowerInitialPause => cowerInitialPause;
        public float DebrisInterval => debrisInterval;
        public float ObstructionResponse => obstructionResponse;
        public float SignalHeartbeat => signalHeartbeat;
        public float MinimumSignalGain => minimumSignalGain;
        public float MinimumSignalSeconds => minimumSignalSeconds;
        public float MaximumSignalSeconds => maximumSignalSeconds;
        public EnemyDeathAudioSet DeathSounds => deathSounds;
        public float EyeBurnGain => eyeBurnGain;
        public int MovementVoices => movementVoices;
        public int IdleVoices => idleVoices;
        public int DeathVoices => deathVoices;


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
