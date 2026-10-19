using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Door access feedback")]
    public sealed class DoorAccessFeedbackProfile : ScriptableObject
    {
        public AudioClip denied;
        public AudioClip granted;
        [Min(0)] public float confirmationDelay = .24f;
        [Min(.1f)] public float denialCooldown = .8f;
        [Range(0, 1)] public float volume = .32f;
        [Min(1)] public float hearingDistance = 7;
    }
}
