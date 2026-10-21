using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Unity transform/Physics2D adapter, separate from input, ammunition and rendering.</summary>
    public sealed class WeaponAimEnvironment2D
    {
        private readonly WeaponAimRig2D rig;
        private readonly IWeaponAimSource2D source;
        private readonly LayerMask blockers;
        public bool HasForwardAim => source.UsesTarget && source.HasTarget && rig.Muzzle != null && rig.Pivot != null
            && Vector2.Dot(source.Target - (Vector2)rig.Muzzle.position, rig.Pivot.up) > .025f;

        public WeaponAimEnvironment2D(WeaponAimRig2D rig, IWeaponAimSource2D source, LayerMask blockers)
        {
            this.rig = rig;
            this.source = source;
            this.blockers = blockers;
        }

        public void UpdateRotation()
        {
            if (rig.Pivot == null)
            {
                return;
            }

            if (source.UsesTarget)
            {
                if (!source.HasTarget || rig.Muzzle == null)
                {
                    return;
                }

                Vector2 offset = source.Target - (Vector2)rig.Pivot.position;
                float distance = offset.magnitude;
                float lateral = Vector2.Dot(rig.Muzzle.position - rig.Pivot.position, rig.Pivot.right);
                if (distance <= Mathf.Abs(lateral) + .001f)
                {
                    return;
                }

                float heading = Mathf.Atan2(offset.y, offset.x)
                    + Mathf.Asin(Mathf.Clamp(lateral / distance, -.999f, .999f));
                rig.Pivot.rotation = Quaternion.Euler(0, 0, heading * Mathf.Rad2Deg - 90);
            }
            else
            {
                Vector2 direction = source.FallbackDirection;
                rig.Pivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
            }
        }

        public bool TryBuildShot(float range, out WeaponShotRequest request, out RaycastHit2D hit)
        {
            request = default;
            hit = default;
            if (rig.Owner == null || rig.Muzzle == null || (source.UsesTarget && !HasForwardAim))
            {
                return false;
            }

            Vector2 origin = rig.Muzzle.position;
            Vector2 target = source.UsesTarget ? source.Target : origin + source.FallbackDirection * range;
            Vector2 direction = (target - origin).normalized;
            hit = Physics2D.Linecast(rig.Owner.position, origin, blockers.value | (1 << 9));
            bool blocked = hit.collider != null;
            if (blocked)
            {
                origin = hit.point - direction * .05f;
            }

            request = new WeaponShotRequest(origin, direction, target, blocked);
            return request.IsValid;
        }
    }
}
