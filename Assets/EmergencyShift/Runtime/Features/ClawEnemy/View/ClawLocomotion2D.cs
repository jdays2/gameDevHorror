using EmergencyShift.Features.ClawEnemy.Logic;
using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.View
{
    public sealed class ClawLocomotion2D : IClawLocomotion
    {
        private readonly EnemyMotor2D motor;
        private readonly EnemyPatrol2D patrol;

        public ClawLocomotion2D(EnemyMotor2D motor, EnemyPatrol2D patrol)
        {
            this.motor = motor;
            this.patrol = patrol;
        }

        public Vector2 Follow(Vector2 goal, float speed) => motor.Follow(goal, speed);
        public void Move(Vector2 velocity, bool separateFromNeighbours) => motor.Move(velocity, separateFromNeighbours);
        public Vector2 Patrol(ClawEnemyObservation frame) => patrol != null ? patrol.Step(frame.Position, frame.Radius, frame.Solids, frame.DeltaTime) : Vector2.zero;
    }
}
