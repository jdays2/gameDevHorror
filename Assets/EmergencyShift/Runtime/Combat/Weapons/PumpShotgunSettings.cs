using System;
using EmergencyShift.Core.Randomness;
using UnityEngine;

namespace EmergencyShift
{
    public sealed class PumpShotgunSettings : MagazineWeaponSettings
    {
        public override int ProjectileCount { get; }
        public float SpreadDegrees { get; }
        public float PumpSeconds { get; }
        public float PumpCueDelay { get; }

        public PumpShotgunSettings(MagazineWeaponSettings magazine, int pellets, float spreadDegrees,
            float pumpSeconds, float pumpCueDelay) : base(magazine, magazine.MagazineCapacity, magazine.ReloadSeconds)
        {
            if (pellets < 2 || pellets > 32 || !float.IsFinite(spreadDegrees) || spreadDegrees <= 0 || spreadDegrees > 80
                || !float.IsFinite(pumpSeconds) || pumpSeconds < .05f || !float.IsFinite(pumpCueDelay)
                || pumpCueDelay < 0 || pumpCueDelay >= pumpSeconds || Automatic)
            {
                throw new ArgumentException("Invalid pump shotgun settings.");
            }

            ProjectileCount = pellets;
            SpreadDegrees = spreadDegrees;
            PumpSeconds = pumpSeconds;
            PumpCueDelay = pumpCueDelay;
        }

        public override WeaponCycle CreateCycle(int cartridges) => new PumpShotgunCycle(this, cartridges);

        public override Vector2 ProjectileDirection(Vector2 heading, int index, IRandomSource random)
        {
            if (index < 0 || index >= ProjectileCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            // Preserve the independent triangular spread inside the same full angle.
            float offset = (float)random.NextUnit() + (float)random.NextUnit() - 1;
            return Quaternion.Euler(0, 0, offset * SpreadDegrees * .5f) * heading;
        }
    }
}
