using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Weapons/Detachable magazine weapon")]
    public sealed class MagazineWeaponDefinition : WeaponDefinition
    {
        [Min(1)]
        [SerializeField]
        private int magazineCapacity = 8;
        [Min(.05f)]
        [SerializeField]
        private float reloadSeconds = 1.4f;

        public int MagazineCapacity => magazineCapacity;
        public float ReloadSeconds => reloadSeconds;

        public override WeaponSettings ToSettings()
        {
            Validate();
            return new MagazineWeaponSettings(base.ToSettings(), magazineCapacity, reloadSeconds);
        }

        public override void Validate()
        {
            base.Validate();
            if (magazineCapacity < 1 || magazineCapacity > MaximumAmmo ||
                !float.IsFinite(reloadSeconds) || reloadSeconds < .05f)
            {
                throw new InvalidOperationException("Invalid magazine weapon tuning: " + name);
            }
        }
    }
}
