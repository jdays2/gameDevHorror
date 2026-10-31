using EmergencyShift.Features.Enemies.Logic;
using System;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    /// <summary>Resolve the route/crowd before steering; constrain final forward motion without strafing.</summary>
    public sealed class ScorpionMotionAdapter : IEnemyBrainMotor
    {
        private readonly EnemyMotor2D motor;
        public ScorpionMotionAdapter(EnemyMotor2D motor) => this.motor = motor != null ? motor : throw new ArgumentNullException(nameof(motor));
        public Vector2 Follow(Vector2 goal, float speed) => motor.ResolveVelocity(motor.Follow(goal, speed));
        public void Move(Vector2 velocity, bool separateFromNeighbours)
        {
            Vector2 limited = motor.LimitForwardVelocity(velocity);
            Vector2 safe = limited.sqrMagnitude > .0001f
                ? motor.FixedHeadingVelocity(limited.normalized, limited.magnitude) : Vector2.zero;
            motor.Move(safe, false);
        }
    }
}
