using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public readonly struct CollectWeaponLootCommand<T> : ICommand where T : struct, IWeaponLootOperation
    {
        private readonly WeaponLootController controller;
        private readonly T operation;

        public CollectWeaponLootCommand(WeaponLootController controller, T operation)
        {
            this.controller = controller;
            this.operation = operation;
        }

        public CommandResult CanExecute() => controller != null && controller.CanCollect && operation.CanApply
            ? CommandResult.Ok() : CommandResult.Fail("Weapon loot is consumed, busy or unavailable.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            return result.Success ? controller.Apply(operation) : result;
        }
    }
}
