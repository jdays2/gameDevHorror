using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Pickups.Logic
{
    public readonly struct CollectPickupCommand<T> : ICommand where T : struct, IPickupOperation
    {
        private readonly SingleUsePickupModel model;
        private readonly T operation;

        public CollectPickupCommand(SingleUsePickupModel model, T operation)
        {
            this.model = model;
            this.operation = operation;
        }

        public CommandResult CanExecute() => model != null && !model.Collected && !model.Collecting && operation.CanApply
            ? CommandResult.Ok() : CommandResult.Fail("Pickup is consumed, busy or unavailable to this actor.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }

            model.Begin();
            try
            {
                if (!operation.Apply())
                {
                    return CommandResult.Fail("Inventory rejected the pickup effect.");
                }

                model.Commit();
                return CommandResult.Ok();
            }
            finally
            {
                model.End();
            }
        }
    }
}
