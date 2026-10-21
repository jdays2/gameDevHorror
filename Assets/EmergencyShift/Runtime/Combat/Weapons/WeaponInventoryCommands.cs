using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift
{
    public sealed class AcquireWeaponCommand : ICommand
    {
        private readonly WeaponInventory inventory;
        private readonly WeaponSettings definition;
        private readonly int cartridges;

        public AcquireWeaponCommand(WeaponInventory inventory, WeaponSettings definition, int cartridges)
        {
            this.inventory = inventory;
            this.definition = definition;
            this.cartridges = cartridges;
        }

        public CommandResult CanExecute() => inventory != null
            ? inventory.CheckAcquire(definition) : CommandResult.Fail("Missing weapon inventory.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                inventory.ApplyAcquire(definition, cartridges);
            }

            return result;
        }
    }

    public sealed class EquipWeaponCommand : ICommand
    {
        private readonly WeaponInventory inventory;
        private readonly int slot;

        public EquipWeaponCommand(WeaponInventory inventory, int slot)
        {
            this.inventory = inventory;
            this.slot = slot;
        }

        public CommandResult CanExecute() => inventory != null
            ? inventory.CheckEquip(slot) : CommandResult.Fail("Missing weapon inventory.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                inventory.ApplyEquip(slot);
            }

            return result;
        }
    }

    public sealed class ReplaceWeaponDefinitionCommand : ICommand
    {
        private readonly WeaponInventory inventory;
        private readonly WeaponSettings definition;

        public ReplaceWeaponDefinitionCommand(WeaponInventory inventory, WeaponSettings definition)
        {
            this.inventory = inventory;
            this.definition = definition;
        }

        public CommandResult CanExecute() => inventory != null
            ? inventory.CheckReplacement(definition) : CommandResult.Fail("Missing weapon inventory.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                inventory.ApplyReplacement(definition);
            }

            return result;
        }
    }

    public sealed class RestoreWeaponInventoryCommand : ICommand
    {
        private readonly WeaponInventory inventory;
        private readonly WeaponInventorySnapshot state;
        private readonly WeaponSettingsCatalog catalog;
        private readonly float now;

        public RestoreWeaponInventoryCommand(WeaponInventory inventory, WeaponInventorySnapshot state,
            WeaponSettingsCatalog catalog, float now)
        {
            this.inventory = inventory;
            this.state = state;
            this.catalog = catalog;
            this.now = now;
        }

        public CommandResult CanExecute()
        {
            if (inventory == null || !WeaponCycle.ValidNow(now))
            {
                return CommandResult.Fail("Missing inventory or invalid simulation time.");
            }

            try
            {
                WeaponInventory.Validate(state, catalog);
                return CommandResult.Ok();
            }
            catch (InvalidOperationException exception)
            {
                return CommandResult.Fail(exception.Message);
            }
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                inventory.ApplyRestore(state, catalog, now);
            }

            return result;
        }
    }
}
