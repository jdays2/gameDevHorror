using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.LevelObjectives.Config;

namespace EmergencyShift.Features.LevelObjectives.Logic
{
    public sealed class ObjectiveProgressController
    {
        private readonly ObjectiveProgressModel model;
        private readonly IObjectiveProgressView view;
        public int Phase => model.Phase;
        public string Text => model.Text;

        public ObjectiveProgressController(ObjectiveProgressModel model, IObjectiveProgressView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public CommandResult Configure(ObjectiveRuleSet rules) => new ConfigureObjectivesCommand(model, rules).Execute();

        public CommandResult Refresh(IObjectiveFacts facts)
        {
            CommandResult result = new RefreshObjectivesCommand(model, facts).Execute();
            if (result.Success)
            {
                view.ObjectivesRefreshed();
            }

            return result;
        }
    }
}
