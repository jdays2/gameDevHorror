using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>Execution of this actor's routes and steering; no health, senses or presentation services.</summary>
    public interface IStalkerLocomotion : IEnemyBrainMotor
    {
        Vector2 ClearDirection(Vector2 preferred);
        bool CanStand(Vector2 goal);
        bool Clear(Vector2 from, Vector2 to);
        Vector2 Patrol(Vector2 at, float radius, int solids, float deltaTime);
        void ResetPatrol();
    }
}
