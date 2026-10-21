using System;

namespace EmergencyShift.Features.Enemies.Config
{
    public sealed class EnemyMeleeSettings
    {
        public EnemyMeleeSettings(int damage, float attackDistance, float duration, float cooldown, float lungeSpeed,
            float biteDistance, float lungeStart, float lungeEnd, float strikeAt, float arcDegrees)
        {
            if (damage < 1 || !Positive(attackDistance) || !Positive(duration) || !Positive(lungeSpeed) ||
                !Positive(biteDistance) || !float.IsFinite(cooldown) || cooldown < 0 ||
                !float.IsFinite(lungeStart) || !float.IsFinite(lungeEnd) || !float.IsFinite(strikeAt) ||
                lungeStart < 0 || lungeStart > strikeAt || strikeAt > lungeEnd || lungeEnd > 1 ||
                !float.IsFinite(arcDegrees) || arcDegrees <= 0 || arcDegrees > 360)
            {
                throw new ArgumentException("Invalid melee values or attack timeline ordering.");
            }

            Damage = damage;
            AttackDistance = attackDistance;
            Duration = duration;
            Cooldown = cooldown;
            LungeSpeed = lungeSpeed;
            BiteDistance = biteDistance;
            LungeStart = lungeStart;
            LungeEnd = lungeEnd;
            StrikeAt = strikeAt;
            ArcDegrees = arcDegrees;
        }

        public int Damage { get; }
        public float AttackDistance { get; }
        public float Duration { get; }
        public float Cooldown { get; }
        public float LungeSpeed { get; }
        public float BiteDistance { get; }
        public float LungeStart { get; }
        public float LungeEnd { get; }
        public float StrikeAt { get; }
        public float ArcDegrees { get; }

        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
