using UnityEngine;
namespace EmergencyShift
{
    [CreateAssetMenu(menuName="Emergency Shift/Door audio set")]
    public sealed class DoorAudioSet : ScriptableObject
    {
        public AudioClip[] openStart, closeStart, stop;
        public AudioClip slideLoop;
    }
}
