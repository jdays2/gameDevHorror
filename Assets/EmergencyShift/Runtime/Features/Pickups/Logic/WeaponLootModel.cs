using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public sealed class WeaponLootModel
    {
        public WeaponLootModel(bool isAmmo, int cartridges)
        {
            if (cartridges < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cartridges));
            }

            IsAmmo = isAmmo;
            Remaining = cartridges;
        }

        public bool IsAmmo { get; private set; }
        public int Remaining { get; private set; }
        public bool Collected { get; private set; }
        public bool Busy { get; private set; }
        internal void Begin() => Busy = true;
        internal void End() => Busy = false;

        internal CommandResult Commit(WeaponLootGrant grant)
        {
            if (!grant.Applied || (IsAmmo && (grant.AmmoReceived <= 0 || grant.AmmoReceived > Remaining)))
            {
                return CommandResult.Fail("Inventory did not accept a valid amount from this pickup.");
            }

            if (IsAmmo)
            {
                Remaining -= grant.AmmoReceived;
            }

            Collected = !IsAmmo || Remaining == 0;
            return CommandResult.Ok();
        }

        internal void Restore(bool consumed, int remaining)
        {
            Collected = consumed;
            Remaining = remaining;
        }

        internal void Configure(bool isAmmo, int cartridges)
        {
            IsAmmo = isAmmo;
            Remaining = cartridges;
        }
    }
}
