using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.Logic
{
    public interface IClawLocomotion : IEnemyBrainMotor
    {
        Vector2 Patrol(ClawEnemyObservation frame);
    }
}
