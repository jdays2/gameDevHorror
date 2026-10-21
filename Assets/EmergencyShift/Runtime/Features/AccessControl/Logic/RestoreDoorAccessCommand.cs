using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class RestoreDoorAccessCommand : ICommand
    {
        private readonly DoorAccessModel model;
        private readonly bool unlocked;

        public RestoreDoorAccessCommand(DoorAccessModel model, bool unlocked)
        {
            this.model = model;
            this.unlocked = unlocked;
        }

        public CommandResult CanExecute() => model != null
            ? CommandResult.Ok() : CommandResult.Fail("Missing access reader.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(unlocked);
            }

            return result;
        }
    }
}
