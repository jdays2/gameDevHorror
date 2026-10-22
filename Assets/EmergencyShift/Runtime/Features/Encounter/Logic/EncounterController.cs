using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Encounter.Logic
{
    public sealed class EncounterController
    {
        private readonly EncounterModel model;
        private readonly IEncounterView view;
        public bool Activated => model.Activated;

        public EncounterController(EncounterModel model, IEncounterView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public CommandResult Activate()
        {
            CommandResult result = new ActivateEncounterCommand(model).Execute();
            if (result.Success)
            {
                view.ActivateActors();
            }

            return result;
        }

        public CommandResult Restore(bool activated) => new RestoreEncounterCommand(model, activated).Execute();
    }
}
