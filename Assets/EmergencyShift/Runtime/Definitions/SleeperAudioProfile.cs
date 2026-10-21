using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Sleeper audio")]
    public sealed class SleeperAudioProfile : ScriptableObject
    {
        [SerializeField]
        private AudioClip sleeping;
        [SerializeField]
        private AudioClip waking;
        [SerializeField]
        private AudioClip pain;
        [SerializeField]
        private AudioClip rupture;
        [SerializeField]
        private EnemyDeathAudioSet deathSounds;
        [Tooltip("Reuse accepted flesh/contact foley without inheriting the Stalker brain or its voice logic.")]
        [SerializeField]
        private StalkerAudioSet contactSounds;
        [Range(0, 1)]
        [SerializeField]
        private float breathingVolume = .36f;
        [Range(0, 1)]
        [SerializeField]
        private float voiceVolume = .85f;
        [Range(0, 1)]
        [SerializeField]
        private float contactVolume = .32f;
        [Min(.05f)]
        [SerializeField]
        private float painCooldown = .6f;
        [Min(.1f)]
        [SerializeField]
        private float distancePerContact = .44f;
        [Min(.1f)]
        [SerializeField]
        private float fullVolumeDistance = 3;
        [Min(1)]
        [SerializeField]
        private float hearingDistance = 12;
        [Min(1)]
        [SerializeField]
        private float panDistance = 6;
        [Range(0, 1)]
        [SerializeField]
        private float maximumPan = .75f;
        [Range(0, 1)]
        [SerializeField]
        private float blockedVolume = .45f;
        [Range(200, 22000)]
        [SerializeField]
        private float blockedCutoff = 1300;
        [Min(.05f)]
        [SerializeField]
        private float obstructionInterval = .15f;
        public AudioClip Sleeping => sleeping;
        public AudioClip Waking => waking;
        public AudioClip Pain => pain;
        public AudioClip Rupture => rupture;
        public EnemyDeathAudioSet DeathSounds => deathSounds;
        public StalkerAudioSet ContactSounds => contactSounds;
        public float BreathingVolume => breathingVolume;
        public float VoiceVolume => voiceVolume;
        public float ContactVolume => contactVolume;
        public float PainCooldown => painCooldown;
        public float DistancePerContact => distancePerContact;
        public float FullVolumeDistance => fullVolumeDistance;
        public float HearingDistance => hearingDistance;
        public float PanDistance => panDistance;
        public float MaximumPan => maximumPan;
        public float BlockedVolume => blockedVolume;
        public float BlockedCutoff => blockedCutoff;
        public float ObstructionInterval => obstructionInterval;

        public void Validate()
        {
            if (!AudioProfileValidation.Unit(breathingVolume) || !AudioProfileValidation.Unit(voiceVolume) ||
                !AudioProfileValidation.Unit(contactVolume) || !AudioProfileValidation.AtLeast(painCooldown, .05f) ||
                !AudioProfileValidation.AtLeast(distancePerContact, .1f) || !AudioProfileValidation.AtLeast(fullVolumeDistance, .1f) ||
                !AudioProfileValidation.AtLeast(hearingDistance, 1) || hearingDistance < fullVolumeDistance ||
                !AudioProfileValidation.AtLeast(panDistance, 1) || !AudioProfileValidation.Unit(maximumPan) ||
                !AudioProfileValidation.Unit(blockedVolume) || !AudioProfileValidation.AtLeast(blockedCutoff, 200) ||
                blockedCutoff > 22000 || !AudioProfileValidation.AtLeast(obstructionInterval, .05f))
            {
                throw new InvalidOperationException("Invalid Sleeper audio profile: " + name);
            }
        }
    }
}
