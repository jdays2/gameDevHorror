using System;
using EmergencyShift.Core.Randomness;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Per-owner loadout, gameplay RNG and transient volley identity.</summary>
    public sealed class WeaponOwnerModel
    {
        private readonly IRandomSource random;
        public WeaponInventory Inventory { get; } = new WeaponInventory();
        public int ShotsFired { get; private set; }
        public WeaponShotPlan LastShot { get; private set; }

        public WeaponOwnerModel(IRandomSource random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        internal WeaponShotPlan ApplyShot(float now, WeaponShotRequest request)
        {
            WeaponCycle cycle = Inventory.ActiveCycle;
            if (!cycle.TryFire(now))
            {
                return null;
            }

            WeaponSettings settings = cycle.Settings;
            int count = request.MuzzleBlocked ? 0 : settings.ProjectileCount;
            var directions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                directions[i] = settings.ProjectileDirection(request.Direction, i, random);
            }

            ShotsFired++;
            LastShot = new WeaponShotPlan(request, ShotsFired, settings, cycle.EjectsOnShot, directions);
            return LastShot;
        }
    }
}
