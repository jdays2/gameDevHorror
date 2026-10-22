using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Encounter.Logic
{
    public readonly struct RestoreEncounterCommand : ICommand
    {
        private readonly EncounterModel model;
        private readonly bool activated;

        public RestoreEncounterCommand(EncounterModel model, bool activated)
        {
            this.model = model;
            this.activated = activated;
        }

        public CommandResult CanExecute() => model != null
            ? CommandResult.Ok() : CommandResult.Fail("Encounter is missing.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(activated);
            }

            return result;
        }
    }
}
