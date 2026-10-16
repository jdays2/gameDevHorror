using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Weapons/Detachable magazine weapon")]
    public sealed class MagazineWeaponDefinition : WeaponDefinition
    {
        [Min(1)]
        public int magazineCapacity = 8;
        [Min(.05f)]
        public float reloadSeconds = 1.4f;

        public override WeaponCycle CreateCycle(int cartridges)
        {
            Validate();
            return new MagazineWeaponCycle(this, cartridges, magazineCapacity, reloadSeconds);
        }

        public override void Validate()
        {
            base.Validate();
            if (magazineCapacity < 1 || magazineCapacity > maximumAmmo ||
                !float.IsFinite(reloadSeconds) || reloadSeconds < .05f)
            {
                throw new InvalidOperationException("Invalid magazine weapon tuning: " + name);
            }
        }
    }
}
