using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    public sealed class EnemyMeleeEnvironment2D : IEnemyMeleeEnvironment
    {
        private readonly EnemyPerception2D senses;
        private readonly EnemyMotor2D motor;

        public EnemyMeleeEnvironment2D(EnemyPerception2D senses, EnemyMotor2D motor)
        {
            this.senses = senses;
            this.motor = motor;
        }

        public EnemyMeleeObservation Observe(Vector2 at, Vector2 heading, bool fixedHeading)
        {
            if (senses.Target == null)
            {
                return default;
            }

            Vector2 offset = (Vector2)senses.Target.position - at;
            return new EnemyMeleeObservation(senses.TargetHealth != null, senses.TargetAlive,
                offset.sqrMagnitude, fixedHeading ? Vector2.Angle(heading, offset) : 0);
        }

        public bool HasLineOfSight(Vector2 at) => senses.CanSeeTarget(at);

        public Vector2 LungeVelocity(Vector2 heading, float speed, bool fixedHeading) => fixedHeading
            ? motor.FixedHeadingVelocity(heading, speed) : motor.ClearDirection(heading) * speed;

        public bool TryDamage(int amount, Vector2 at, Vector2 heading) =>
            senses.TargetHealth != null && senses.TargetHealth.TryTakeDamage(amount, at, heading);
    }
}
