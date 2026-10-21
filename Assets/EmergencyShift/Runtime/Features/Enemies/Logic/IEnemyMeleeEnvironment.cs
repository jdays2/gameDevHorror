using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public interface IEnemyMeleeEnvironment
    {
        EnemyMeleeObservation Observe(Vector2 at, Vector2 heading, bool fixedHeading);
        bool HasLineOfSight(Vector2 at);
        Vector2 LungeVelocity(Vector2 heading, float speed, bool fixedHeading);
        bool TryDamage(int amount, Vector2 at, Vector2 heading);
    }
}
