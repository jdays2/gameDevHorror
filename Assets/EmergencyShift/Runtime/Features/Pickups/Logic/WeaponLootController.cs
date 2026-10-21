using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public sealed class WeaponLootController
    {
        private readonly WeaponLootModel model;
        public WeaponLootController(WeaponLootModel model) => this.model = model ?? throw new ArgumentNullException(nameof(model));
        public bool CanCollect => !model.Collected && !model.Busy;
        public bool Collected => model.Collected;
        public int Remaining => model.Remaining;

        public CommandResult TryCollect<T>(T operation) where T : struct, IWeaponLootOperation => new CollectWeaponLootCommand<T>(this, operation).Execute();
        public CommandResult Restore(bool consumed, int remaining) => new RestoreWeaponLootCommand(model, consumed, remaining).Execute();
        public CommandResult Configure(bool isAmmo, int cartridges) => new ConfigureWeaponLootCommand(model, isAmmo, cartridges).Execute();

        internal CommandResult Apply<T>(T operation) where T : struct, IWeaponLootOperation
        {
            model.Begin();
            try
            {
                WeaponLootGrant grant = operation.Apply(model.IsAmmo, model.Remaining);
                CommandResult result = model.Commit(grant);
                if (result.Success)
                {
                    operation.Present(model.IsAmmo, grant.AmmoReceived, model.Collected);
                }

                return result;
            }
            finally
            {
                model.End();
            }
        }
    }
}
