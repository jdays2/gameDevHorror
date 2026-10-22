using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Door audio set")]
    public sealed class DoorAudioSet : ScriptableObject
    {

        [Header("Shared playback tuning")]
        [SerializeField]
        private float volume = .85f;
        [SerializeField]
        private float movementVolume = .4f;
        [SerializeField]
        private float hearingDistance = 12;
        [SerializeField]
        private float fullVolumeDistance = 2;
        [SerializeField]
        private float panDistance = 6;
        [SerializeField]
        private float maximumPan = .75f;
        [SerializeField]
        private AudioClip[] openStart;
        [SerializeField]
        private AudioClip[] closeStart;
        [SerializeField]
        private AudioClip[] stop;
        [SerializeField]
        private AudioClip slideLoop;

        public float Volume => volume;
        public float MovementVolume => movementVolume;
        public float HearingDistance => hearingDistance;
        public float FullVolumeDistance => fullVolumeDistance;
        public float PanDistance => panDistance;
        public float MaximumPan => maximumPan;
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
