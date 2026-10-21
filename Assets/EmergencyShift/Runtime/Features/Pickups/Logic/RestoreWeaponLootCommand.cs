using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public readonly struct RestoreWeaponLootCommand : ICommand
    {
        private readonly WeaponLootModel model;
        private readonly bool consumed;
        private readonly int remaining;

        public RestoreWeaponLootCommand(WeaponLootModel model, bool consumed, int remaining)
        {
            this.model = model;
            this.consumed = consumed;
            this.remaining = remaining;
        }

        public CommandResult CanExecute() => model != null && !model.Busy && remaining >= 0
            ? CommandResult.Ok() : CommandResult.Fail("Weapon loot restore requires valid remaining ammo and no active operation.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(consumed, remaining);
            }

            return result;
        }
    }
}
