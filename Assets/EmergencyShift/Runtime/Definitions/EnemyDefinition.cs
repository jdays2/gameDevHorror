using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Shared tuning only. Current health, aggro and timers belong to an instance.</summary>
    [CreateAssetMenu(menuName="Emergency Shift/Enemies/Enemy definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string displayName="Сталкер";
        [Min(1)] public int maximumHealth=2;
        [Min(.1f)] public float pursuitSpeed=1.5f;
        [Min(0)] public float hitStaggerSeconds=.09f, hitPushSpeed=1.6f;
        [Header("Melee attack")]
        [Min(1)] public int attackDamage=1;
        [Min(.1f)] public float attackDistance=1.28f, attackDuration=.8f, attackCooldown=1.2f;
        [Min(.1f)] public float lungeSpeed=2.7f, biteDistance=1.4f;
        [Range(0,1)] public float lungeStart=.25f, lungeEnd=.48f, strikeAt=.42f;
    }
}
