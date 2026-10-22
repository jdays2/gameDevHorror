using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Door audio set")]
    public sealed class DoorAudioSet : ScriptableObject
    {

        [Header("Shared playback tuning")]
        [SerializeField]
        private float volume = .85f;
        public float Volume => volume;
        [SerializeField]
        private float movementVolume = .4f;
        public float MovementVolume => movementVolume;
        [SerializeField]
        private float hearingDistance = 12;
        public float HearingDistance => hearingDistance;
        [SerializeField]
        private float fullVolumeDistance = 2;
        public float FullVolumeDistance => fullVolumeDistance;
        [SerializeField]
        private float panDistance = 6;
        public float PanDistance => panDistance;
        [SerializeField]
        private float maximumPan = .75f;
        public float MaximumPan => maximumPan;
        [SerializeField]
        private AudioClip[] openStart;
        [SerializeField]
        private AudioClip[] closeStart;
        [SerializeField]
        private AudioClip[] stop;
        [SerializeField]
        private AudioClip slideLoop;
        public AudioClipBank OpenStart => openStart;
        public AudioClipBank CloseStart => closeStart;
        public AudioClipBank Stop => stop;
        public AudioClip SlideLoop => slideLoop;

        public void Validate()
        {
            if (!AudioProfileValidation.Unit(volume) ||
                !AudioProfileValidation.Unit(movementVolume) ||
                !AudioProfileValidation.Positive(hearingDistance) ||
                !AudioProfileValidation.AtLeast(fullVolumeDistance, 0) ||
                !AudioProfileValidation.Positive(panDistance) ||
                !AudioProfileValidation.Unit(maximumPan) || fullVolumeDistance >= hearingDistance)
            {
                throw new System.InvalidOperationException("Invalid playback tuning: " + name);
            }
        }
    }
}
