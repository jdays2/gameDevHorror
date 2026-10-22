using UnityEngine;

namespace EmergencyShift.Features.Doors.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Door rules")]
    public sealed class DoorRulesProfile : ScriptableObject
    {
        [SerializeField] private float duration = DoorRulesSettings.LegacyDuration;
        [SerializeField] private float interactionRange = DoorRulesSettings.LegacyRange;
        public DoorRulesSettings ToSettings() => new DoorRulesSettings(duration, interactionRange);
    }
}
