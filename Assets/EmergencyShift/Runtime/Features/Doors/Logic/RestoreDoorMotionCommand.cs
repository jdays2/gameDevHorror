using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Doors.Logic
{
    public readonly struct RestoreDoorMotionCommand : ICommand
    {
        private readonly DoorMotionModel model;
        private readonly bool open;

        public RestoreDoorMotionCommand(DoorMotionModel model, bool open)
        {
            this.model = model;
            this.open = open;
        }

        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Missing door model.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(open);
            }

            return result;
        }
    }
}
