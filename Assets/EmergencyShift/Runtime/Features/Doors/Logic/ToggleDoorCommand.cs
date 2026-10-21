using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Doors.Logic
{
    public readonly struct ToggleDoorCommand : ICommand
    {
        private readonly DoorMotionModel model;
        private readonly bool unlocked;
        private readonly float delay;

        public ToggleDoorCommand(DoorMotionModel model, bool unlocked, float confirmationDelay = 0)
        {
            this.model = model;
            this.unlocked = unlocked;
            delay = confirmationDelay;
        }

        public CommandResult CanExecute() => model != null && model.CanInteract && (model.IsOpen || unlocked)
            && float.IsFinite(delay) && delay >= 0
            ? CommandResult.Ok() : CommandResult.Fail("Door is busy, locked or confirmation delay is invalid.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.RequestToggle(delay);
            }

            return result;
        }
    }
}
