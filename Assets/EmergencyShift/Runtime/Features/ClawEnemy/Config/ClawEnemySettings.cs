using System;

namespace EmergencyShift.Features.ClawEnemy.Config
{
    public sealed class ClawEnemySettings
    {
        public ClawEnemySettings(float speed, float attackSeconds, float staggerSeconds, float pushSpeed)
        {
            if (!float.IsFinite(speed) || speed <= 0 || !float.IsFinite(attackSeconds) || attackSeconds <= 0 ||
                !float.IsFinite(staggerSeconds) || staggerSeconds < 0 || !float.IsFinite(pushSpeed) || pushSpeed < 0)
            {
                throw new ArgumentException("Invalid claw enemy movement/attack settings.");
            }
            Speed = speed;
            AttackSeconds = attackSeconds;
            StaggerSeconds = staggerSeconds;
            PushSpeed = pushSpeed;
        }

        public float Speed { get; }
        public float AttackSeconds { get; }
        public float StaggerSeconds { get; }
        public float PushSpeed { get; }
    }
}
