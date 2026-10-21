using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public sealed class SingleUsePickupController
    {
        private readonly SingleUsePickupModel model;
        public SingleUsePickupController(SingleUsePickupModel model) => this.model = model ?? throw new ArgumentNullException(nameof(model));
        public bool Collected => model.Collected;

        public CommandResult TryCollect<T>(T operation) where T : struct, IPickupOperation
        {
            CommandResult result = new CollectPickupCommand<T>(model, operation).Execute();
            if (result.Success)
            {
                operation.Present();
            }

            return result;
        }

        public CommandResult Restore(bool consumed) => new RestorePickupCommand(model, consumed).Execute();
    }
}
