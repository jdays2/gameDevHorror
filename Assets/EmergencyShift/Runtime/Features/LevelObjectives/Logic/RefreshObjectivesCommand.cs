using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.LevelObjectives.Logic
{
    public readonly struct RefreshObjectivesCommand : ICommand
    {
        private readonly ObjectiveProgressModel model;
        private readonly IObjectiveFacts facts;

        public RefreshObjectivesCommand(ObjectiveProgressModel model, IObjectiveFacts facts)
        {
            this.model = model;
            this.facts = facts;
        }

        public CommandResult CanExecute() => model != null && facts != null
            ? CommandResult.Ok() : CommandResult.Fail("Objectives require a model and inventory facts.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            return result.Success ? model.Refresh(facts) : result;
        }
    }
}
