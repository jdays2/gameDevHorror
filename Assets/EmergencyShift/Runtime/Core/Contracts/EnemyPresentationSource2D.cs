using UnityEngine;

namespace EmergencyShift
{
    public abstract class EnemyPresentationSource2D : MonoBehaviour
    {
        public virtual bool IsWalking => false;
        public virtual bool IsAttacking => false;
        public virtual bool IsOrienting => IsAttacking;
        public virtual bool IsStaggered => false;
        public virtual bool IsCompactWalking => false;
        public virtual float Closure => 0;
        public virtual Vector2 Velocity => Vector2.zero;
        public float MovementSpeed => Velocity.magnitude;
        public virtual Transform Target => null;
        public virtual Vector2 AttackHeading => Target != null ? (Vector2)(Target.position - transform.position) : Vector2.up;
        public abstract bool IsDead
        {
            get;
        }
        public abstract float AttackProgress
        {
            get;
        }
        public abstract float DeathTime
        {
            get;
        }
        public abstract IThreatLight2D LightSource
        {
            get;
        }
        public abstract LayerMask BlockingGeometry
        {
            get;
        }
    }
}
