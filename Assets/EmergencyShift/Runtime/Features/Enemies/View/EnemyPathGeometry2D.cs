using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    public sealed class EnemyPathGeometry2D : IEnemyPathGeometry
    {
        public bool IsBlocked(Vector2 position, float radius, Vector2 direction, float distance, int solids) =>
            Physics2D.CircleCast(position, radius, direction, distance, solids).collider != null;
        public bool CanStand(Vector2 position, float radius, int solids) => Physics2D.OverlapCircle(position, radius, solids) == null;
        public bool LineClear(Vector2 from, Vector2 to, int solids) => Physics2D.Linecast(from, to, solids).collider == null;
    }
}
