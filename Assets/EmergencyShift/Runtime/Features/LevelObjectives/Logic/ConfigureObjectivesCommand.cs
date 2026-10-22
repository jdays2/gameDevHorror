using EmergencyShift.Core.Commands;
using EmergencyShift.Features.LevelObjectives.Config;

namespace EmergencyShift.Features.LevelObjectives.Logic
{
    public readonly struct ConfigureObjectivesCommand : ICommand
    {
        private readonly ObjectiveProgressModel model;
        private readonly ObjectiveRuleSet rules;

        public ConfigureObjectivesCommand(ObjectiveProgressModel model, ObjectiveRuleSet rules)
        {
            this.model = model;
            this.rules = rules;
        }

        public CommandResult CanExecute() => model != null && rules != null
            ? CommandResult.Ok() : CommandResult.Fail("Objectives require valid rules and a model.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Configure(rules);
            }

            return result;
        }
    }
}
