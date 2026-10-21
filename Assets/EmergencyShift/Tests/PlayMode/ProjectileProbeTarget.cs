using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class ProjectileProbeTarget : Damageable2D
    {
        public int Hits { get; private set; }
        public int Damage { get; private set; }
        public override bool IsAlive => true;

        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            Hits++;
            Damage += damage;
        }
    }
}
