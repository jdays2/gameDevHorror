using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public interface IEnemyPathGeometry : IEnemyPatrolEnvironment
    {
        bool CanStand(Vector2 position, float radius, int solids);
        bool LineClear(Vector2 from, Vector2 to, int solids);
    }
}
