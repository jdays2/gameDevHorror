using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Authored geometry of one rig; these references never enter simulation models.</summary>
    public sealed class WeaponAimRig2D
    {
        public Transform Owner { get; }
        public Transform Pivot { get; }
        public Transform Muzzle { get; }

        public WeaponAimRig2D(Transform owner, Transform pivot, Transform muzzle)
        {
            Owner = owner;
            Pivot = pivot;
            Muzzle = muzzle;
        }
    }
}
