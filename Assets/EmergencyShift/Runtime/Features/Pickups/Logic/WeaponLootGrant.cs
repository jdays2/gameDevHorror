namespace EmergencyShift.Features.Pickups.Logic
{
    public readonly struct WeaponLootGrant
    {
        private WeaponLootGrant(bool applied, int ammoReceived)
        {
            Applied = applied;
            AmmoReceived = ammoReceived;
        }

        public bool Applied { get; }
        public int AmmoReceived { get; }
        public static WeaponLootGrant Weapon(bool acquired) => new WeaponLootGrant(acquired, 0);
        public static WeaponLootGrant Ammo(int received) => new WeaponLootGrant(received > 0, received);
    }
}
