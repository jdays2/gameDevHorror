using System;
using UnityEngine;
using EmergencyShift.Features.Enemies.Config;

namespace EmergencyShift
{
    /// <summary>Shared tuning only. Current health, aggro and timers belong to an instance.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Enemy definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Сталкер";
        [SerializeField, Min(1)]
        private int maximumHealth = 2;
        [SerializeField, Min(.1f)]
        private float pursuitSpeed = 1.5f;
        [SerializeField, Min(0)]
        private float hitStaggerSeconds = .09f;
        [SerializeField, Min(0)]
        private float hitPushSpeed = 1.6f;
        [Header("Melee attack")]
        [SerializeField, Min(1)]
        private int attackDamage = 1;
        [SerializeField, Min(.1f)]
        private float attackDistance = 1.28f;
        [SerializeField, Min(.1f)]
        private float attackDuration = .8f;
        [SerializeField, Min(.1f)]
        private float attackCooldown = 1.2f;
        [SerializeField, Min(.1f)]
        private float lungeSpeed = 2.7f;
        [SerializeField, Min(.1f)]
        private float biteDistance = 1.4f;
        [SerializeField, Range(0, 1)]
        private float lungeStart = .25f;
        [SerializeField, Range(0, 1)]
        private float lungeEnd = .48f;
        [SerializeField, Range(0, 1)]
        private float strikeAt = .42f;
        [SerializeField, Range(1, 360), Tooltip("Strike arc for committed fixed-heading attacks. Legacy steering attacks keep their previous contact rule.")]
        private float attackArcDegrees = 120;

        public string DisplayName => displayName;
        public int MaximumHealth => maximumHealth;
        public float PursuitSpeed => pursuitSpeed;
        public float HitStaggerSeconds => hitStaggerSeconds;
        public float HitPushSpeed => hitPushSpeed;
        public float AttackDuration => attackDuration;
        public float AttackCooldown => attackCooldown;
        public float LungeStart => lungeStart;
        public float LungeEnd => lungeEnd;
        public float StrikeAt => strikeAt;

        public EnemyMeleeSettings ToMeleeSettings()
        {
            if (maximumHealth < 1 || !Nonnegative(pursuitSpeed) || !Nonnegative(hitStaggerSeconds) || !Nonnegative(hitPushSpeed))
            {
                throw new InvalidOperationException("Invalid enemy health/movement tuning: " + name);
            }

            return new EnemyMeleeSettings(attackDamage, attackDistance, attackDuration, attackCooldown,
                lungeSpeed, biteDistance, lungeStart, lungeEnd, strikeAt, attackArcDegrees);
        }

        public void Validate() => ToMeleeSettings();
        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
    }
}
