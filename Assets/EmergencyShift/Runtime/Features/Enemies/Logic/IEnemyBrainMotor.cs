using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public interface IEnemyBrainMotor
    {
        Vector2 Follow(Vector2 goal, float speed);
        void Move(Vector2 velocity, bool separateFromNeighbours);
    }
}
