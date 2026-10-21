using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public readonly struct ConfigureWeaponLootCommand : ICommand
    {
        private readonly WeaponLootModel model;
        private readonly bool isAmmo;
        private readonly int cartridges;

        public ConfigureWeaponLootCommand(WeaponLootModel model, bool isAmmo, int cartridges)
        {
            this.model = model;
            this.isAmmo = isAmmo;
            this.cartridges = cartridges;
        }

        public CommandResult CanExecute() => model != null && !model.Busy && cartridges >= 0
            ? CommandResult.Ok() : CommandResult.Fail("Cannot configure active loot or a negative amount.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Configure(isAmmo, cartridges);
            }

            return result;
        }
    }
}
