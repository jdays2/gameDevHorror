using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    public sealed class EnemyBrainMotor2D : IEnemyBrainMotor
    {
        private readonly EnemyMotor2D motor;
        public EnemyBrainMotor2D(EnemyMotor2D motor) => this.motor = motor != null ? motor : throw new ArgumentNullException(nameof(motor));
        public Vector2 Follow(Vector2 goal, float speed) => motor.Follow(goal, speed);
        public void Move(Vector2 velocity, bool separateFromNeighbours) => motor.Move(velocity, separateFromNeighbours);
    }
}
