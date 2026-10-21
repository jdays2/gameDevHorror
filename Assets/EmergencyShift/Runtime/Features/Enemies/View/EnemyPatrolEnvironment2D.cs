using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    public sealed class EnemyPatrolEnvironment2D : IEnemyPatrolEnvironment
    {
        public bool IsBlocked(Vector2 position, float radius, Vector2 direction, float distance, int solids) =>
            Physics2D.CircleCast(position, radius, direction, distance, solids).collider != null;
    }
}
