using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Sleeper audio")]
    public sealed class SleeperAudioProfile : ScriptableObject
    {
        public AudioClip sleeping;
        public AudioClip waking;
        public AudioClip pain;
        public AudioClip rupture;
        [Tooltip("Reuse accepted flesh/contact foley without inheriting the Stalker brain or its voice logic.")]
        public StalkerAudioSet contactSounds;
        [Range(0, 1)] public float breathingVolume = .36f;
        [Range(0, 1)] public float voiceVolume = .85f;
        [Range(0, 1)] public float contactVolume = .32f;
        [Min(.05f)] public float painCooldown = .6f;
        [Min(.1f)] public float distancePerContact = .44f;
        [Min(.1f)] public float fullVolumeDistance = 3;
        [Min(1)] public float hearingDistance = 12;
        [Min(1)] public float panDistance = 6;
        [Range(0, 1)] public float maximumPan = .75f;
        [Range(0, 1)] public float blockedVolume = .45f;
        [Range(200, 22000)] public float blockedCutoff = 1300;
        [Min(.05f)] public float obstructionInterval = .15f;
    }
}
