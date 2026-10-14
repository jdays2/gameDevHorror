using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Combat contract. A projectile does not need to know the enemy species.</summary>
    public abstract class Damageable2D : MonoBehaviour
    {
        public abstract bool IsAlive { get; }
        public virtual bool OwnsHitAudio => false;
        public abstract void ReceiveBulletHit(int damage,Vector2 point,Vector2 direction);
    }
}
