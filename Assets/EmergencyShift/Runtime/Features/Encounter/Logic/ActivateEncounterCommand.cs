using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Encounter.Logic
{
    public readonly struct ActivateEncounterCommand : ICommand
    {
        private readonly EncounterModel model;
        public ActivateEncounterCommand(EncounterModel model) => this.model = model;
        public CommandResult CanExecute() => model != null && !model.Activated
            ? CommandResult.Ok() : CommandResult.Fail("Encounter is missing or already activated.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Activate();
            }

            return result;
        }
    }
}
