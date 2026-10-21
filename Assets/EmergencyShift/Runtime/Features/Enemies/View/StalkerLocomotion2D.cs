using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    public sealed class StalkerLocomotion2D : IStalkerLocomotion
    {
        private readonly EnemyMotor2D motor;
        private readonly EnemyPatrol2D patrol;
        public StalkerLocomotion2D(EnemyMotor2D motor, EnemyPatrol2D patrol)
        {
            this.motor = motor != null ? motor : throw new ArgumentNullException(nameof(motor));
            this.patrol = patrol;
        }

        public Vector2 Follow(Vector2 goal, float speed) => motor.Follow(goal, speed);
        public void Move(Vector2 velocity, bool separateFromNeighbours) => motor.Move(velocity, separateFromNeighbours);
        public Vector2 ClearDirection(Vector2 preferred) => motor.ClearDirection(preferred);
        public bool CanStand(Vector2 goal) => motor.CanStand(goal);
        public bool Clear(Vector2 from, Vector2 to) => motor.Clear(from, to);
        public Vector2 Patrol(Vector2 at, float radius, int solids, float deltaTime) =>
            patrol != null ? patrol.Step(at, radius, solids, deltaTime) : Vector2.zero;
        public void ResetPatrol() => patrol?.ResetRoute();
    }
}
