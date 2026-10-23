using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Combat contract. A projectile does not need to know the enemy species.</summary>
    public abstract class Damageable2D : MonoBehaviour
    {
        public abstract bool IsAlive
        {
            get;
        }

        public abstract void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction);
        public virtual bool TryTakeDamage(int amount, Vector2 point, Vector2 direction)
        {
            if (!IsAlive || amount <= 0)
            {
                return false;
            }

            ReceiveBulletHit(amount, point, direction);
            return true;
        }
    }
}
