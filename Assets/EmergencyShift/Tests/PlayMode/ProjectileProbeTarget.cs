using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class ProjectileProbeTarget : Damageable2D
    {
        public int Hits { get; private set; }
        public int Damage { get; private set; }
        public BulletProjectile2D ObservedProjectile { get; set; }
        public bool DieOnHit { get; set; }
        public bool VisibleFlightAtContact { get; private set; }
        public override bool IsAlive => !DieOnHit || Hits == 0;

        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            Hits++;
            Damage += damage;
            if (ObservedProjectile != null)
            {
                foreach (var renderer in ObservedProjectile.GetComponentsInChildren<SpriteRenderer>())
                {
                    VisibleFlightAtContact |= renderer.enabled;
                }
            }
            if (DieOnHit)
            {
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }
}
