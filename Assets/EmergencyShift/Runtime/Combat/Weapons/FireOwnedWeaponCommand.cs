using EmergencyShift.Core.Commands;

namespace EmergencyShift
{
    public sealed class FireOwnedWeaponCommand : ICommand
    {
        private readonly WeaponOwnerModel model;
        private readonly float now;
        private readonly WeaponShotRequest request;
        public WeaponShotPlan Plan { get; private set; }

        public FireOwnedWeaponCommand(WeaponOwnerModel model, float now, WeaponShotRequest request)
        {
            this.model = model;
            this.now = now;
            this.request = request;
        }

        public CommandResult CanExecute()
        {
            return model != null && request.IsValid && model.Inventory.ActiveCycle != null
                && model.Inventory.ActiveCycle.CanFire(now)
                ? CommandResult.Ok() : CommandResult.Fail("No usable weapon, valid aim or ready shot.");
        }

        public CommandResult Execute()
        {
            Plan = null;
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }

            Plan = model.ApplyShot(now, request);
            return Plan != null ? result : CommandResult.Fail("The weapon rejected the shot.");
        }
    }
}
