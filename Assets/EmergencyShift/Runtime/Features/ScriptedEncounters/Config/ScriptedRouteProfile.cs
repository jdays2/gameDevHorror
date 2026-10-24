using UnityEngine;

namespace EmergencyShift.Features.ScriptedEncounters.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Encounters/Scripted light retreat")]
    public sealed class ScriptedRouteProfile : ScriptableObject
    {
        [SerializeField, Min(.1f)]
        private float speed = 3.8f;
        [SerializeField, Min(0)]
        private float reactionSeconds = .12f;
        [SerializeField, Min(.01f)]
        private float arrivalDistance = .16f;
        [SerializeField, Min(.1f)]
        private float blockedSeconds = 3;
        [SerializeField, Min(.001f)]
        private float progressDistance = .03f;

        public ScriptedRouteSettings ToSettings() => new ScriptedRouteSettings(speed, reactionSeconds, arrivalDistance, blockedSeconds, progressDistance);
    }
}
