using EmergencyShift.Features.Pickups.Logic;

namespace EmergencyShift.Features.Pickups
{
    public sealed class PickupControllerFactory
    {
        public SingleUsePickupController CreateSingleUse() => new SingleUsePickupController(new SingleUsePickupModel());
    }
}
