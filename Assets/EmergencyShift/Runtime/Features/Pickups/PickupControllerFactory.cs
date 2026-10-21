using EmergencyShift.Features.Pickups.Logic;

namespace EmergencyShift.Features.Pickups
{
    public sealed class PickupControllerFactory
    {
        public SingleUsePickupController CreateSingleUse() => new SingleUsePickupController(new SingleUsePickupModel());
        public WeaponLootController CreateWeaponLoot(bool isAmmo, int cartridges) => new WeaponLootController(new WeaponLootModel(isAmmo, cartridges));
    }
}
