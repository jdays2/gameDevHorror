using UnityEngine;

namespace EmergencyShift.Features.Projectiles.Logic
{
    public interface IProjectileFlightEnvironment
    {
        bool Sweep(Vector2 origin, Vector2 direction, float radius, float distance);
        void Move(Vector2 position);
        void Impact(int damage, Vector2 direction);
        void Release();
    }
}
