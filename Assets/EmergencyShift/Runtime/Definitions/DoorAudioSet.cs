using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Door audio set")]
    public sealed class DoorAudioSet : ScriptableObject
    {
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

    }
}
