using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public interface IEnemyPatrolEnvironment
    {
        bool IsBlocked(Vector2 position, float radius, Vector2 direction, float distance, int solids);
    }
}
