using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared tuning only. Current health, aggro and timers belong to an instance.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Enemy definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string displayName = "Сталкер";
        [Min(1)]
        public int maximumHealth = 2;
        [Min(.1f)]
        public float pursuitSpeed = 1.5f;
        [Min(0)]
        public float hitStaggerSeconds = .09f;
        [Min(0)]
        public float hitPushSpeed = 1.6f;
        [Header("Melee attack")]
        [Min(1)]
        public int attackDamage = 1;
        [Min(.1f)]
        public float attackDistance = 1.28f;
        [Min(.1f)]
        public float attackDuration = .8f;
        [Min(.1f)]
        public float attackCooldown = 1.2f;
        [Min(.1f)]
        public float lungeSpeed = 2.7f;
        [Min(.1f)]
        public float biteDistance = 1.4f;
        [Range(0, 1)]
        public float lungeStart = .25f;
        [Range(0, 1)]
        public float lungeEnd = .48f;
        [Range(0, 1)]
        public float strikeAt = .42f;
        [Range(1, 360), Tooltip("Strike arc for committed fixed-heading attacks. Legacy steering attacks keep their previous contact rule.")]
        public float attackArcDegrees = 120;
    }
}
