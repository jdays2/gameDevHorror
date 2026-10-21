using System;

namespace EmergencyShift
{
    public class MagazineWeaponSettings : WeaponSettings
    {
        public int MagazineCapacity { get; }
        public float ReloadSeconds { get; }

        public MagazineWeaponSettings(WeaponSettings common, int magazineCapacity, float reloadSeconds)
            : base(common)
        {
            if (magazineCapacity < 1 || magazineCapacity > MaximumAmmo
                || !float.IsFinite(reloadSeconds) || reloadSeconds < .05f)
            {
                throw new ArgumentException("Invalid magazine settings.");
            }

            MagazineCapacity = magazineCapacity;
            ReloadSeconds = reloadSeconds;
        }

        public override WeaponCycle CreateCycle(int cartridges) => new MagazineWeaponCycle(this, cartridges);
    }
}
