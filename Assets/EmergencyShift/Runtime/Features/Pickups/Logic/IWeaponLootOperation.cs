namespace EmergencyShift.Features.Pickups.Logic
{
    public interface IWeaponLootOperation
    {
        bool CanApply { get; }
        WeaponLootGrant Apply(bool isAmmo, int cartridges);
        void Present(bool isAmmo, int received, bool consumed);
    }
}
