using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Reusable attack timeline and hit delivery. The brain decides when to start/interrupt.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMeleeAttack2D : MonoBehaviour
    {
        private EnemyDefinition tuning;
        private EnemyPerception2D senses;
        private EnemyMotor2D motor;
        private bool struck;
        private bool lunged;
        private float readyAt;
        public event System.Action Lunged;
        public event System.Action<bool> Resolved;
        public void Configure(EnemyDefinition definition, EnemyPerception2D perception, EnemyMotor2D movement)
        {
            tuning = definition;
            senses = perception;
            motor = movement;
        }

        public bool CanStart(Vector2 at)
        {
            if (tuning == null || senses.TargetHealth == null || Time.time < readyAt)
            {
                return false;
            }

            return senses.CanSeeTarget(at) && senses.Within(at, tuning.attackDistance);
        }

        private bool CanBite(Vector2 at)
        {
            return senses.TargetHealth != null && senses.TargetAlive && Vector2.Distance(at, senses.Target.position) < tuning.biteDistance && senses.CanSeeTarget(at);
        }

        public void Begin()
        {
            struck = false;
            lunged = false;
        }

        public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading = false)
        {
            if (!lunged && progress >= tuning.lungeStart)
            {
                lunged = true;
                Lunged?.Invoke();
            }

            Vector2 velocity = Vector2.zero;
            if (progress >= tuning.lungeStart && progress < tuning.lungeEnd)
            {
                velocity = fixedHeading ? motor.FixedHeadingVelocity(heading, tuning.lungeSpeed) : motor.ClearDirection(heading) * tuning.lungeSpeed;
            }
            if (!struck && progress >= tuning.strikeAt)
            {
                struck = true;
                bool landed = false;
                bool inArc = !fixedHeading || (senses.Target != null && Vector2.Angle(heading, (Vector2)senses.Target.position - at) <= tuning.attackArcDegrees * .5f);
                if (inArc && CanBite(at))
                {
                    landed = senses.TargetHealth.TryTakeDamage(tuning.attackDamage, at, heading);
                }

                Resolved?.Invoke(landed);
            }

            return velocity;
        }

        [System.Serializable]
        public sealed class Snapshot
        {
            public bool struck;
            public bool lunged;
            public float cooldown;
        }

        public Snapshot CaptureState()
        {
            return new Snapshot
            {
                struck = struck,
                lunged = lunged,
                cooldown = Mathf.Max(0, readyAt - Time.time)
            };
        }

        public void RestoreState(Snapshot saved)
        {
            struck = saved.struck;
            lunged = saved.lunged;
            readyAt = Time.time + saved.cooldown;
        }

        public void Finish()
        {
            readyAt = Time.time + tuning.attackCooldown;
        }

        public void Interrupt(float delay)
        {
            readyAt = Time.time + delay;
        }
    }
}
