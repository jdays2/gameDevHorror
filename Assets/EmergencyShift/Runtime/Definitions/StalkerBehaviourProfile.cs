using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Stalker behaviour")]
    public sealed class StalkerBehaviourProfile : ScriptableObject
    {
        [Min(.1f)]
        public float retreatSpeed = 3.8f;
        [Min(.1f)]
        public float standOffDistance = 1.05f;
        [Min(.1f)]
        public float recoilSeconds = .35f;
        [Min(.1f)]
        public float retreatSeconds = 1.05f;
        [Min(.1f)]
        public float cooldownSeconds = 3;
        [Min(.1f)]
        public float lightFearDistance = 3.2f;
        [Min(.05f)]
        public float reactionDelay = .45f;
        [Header("Eye burn — cumulative damage, separate from fear")]
        [Min(.05f), Tooltip("Audible contact precedes any visible change, seconds.")]
        public float eyeBurnLeadSeconds = .14f;
        [Min(.1f), Tooltip("Illuminated seconds after the lead-in to reach full damage.")]
        public float eyeBurnSeconds = 1.25f;
        [Min(.1f)]
        public float eyeBurnCueInterval = 1.2f;
    }
}
