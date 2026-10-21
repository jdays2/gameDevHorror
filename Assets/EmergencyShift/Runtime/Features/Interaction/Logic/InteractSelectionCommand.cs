using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Interaction.Logic
{
    public readonly struct InteractSelectionCommand : ICommand
    {
        private readonly InteractionSelectionModel model;
        private readonly IInteractionEnvironment environment;

        public InteractSelectionCommand(InteractionSelectionModel model, IInteractionEnvironment environment)
        {
            this.model = model;
            this.environment = environment;
        }

        public CommandResult CanExecute() => model != null && environment != null && environment.CanAct &&
            model.CurrentIndex >= 0 && environment.IsAvailable(model.CurrentIndex)
            ? CommandResult.Ok() : CommandResult.Fail("No available interaction target or authorized actor.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }

            return environment.Interact(model.CurrentIndex) ? CommandResult.Ok() : CommandResult.Fail("Target rejected interaction.");
        }
    }
}
