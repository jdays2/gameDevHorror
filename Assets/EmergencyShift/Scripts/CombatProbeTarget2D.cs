#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Development-only non-Stalker receiver used by smoke tests, never placed in production scenes.</summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CombatProbeTarget2D : Damageable2D
    {
        public int Hits { get; private set; }
        public int TotalDamage { get; private set; }
        public override bool IsAlive => TotalDamage<100;
        public override void ReceiveBulletHit(int damage,Vector2 point,Vector2 direction){Hits++;TotalDamage+=damage;}
    }
}
#endif
