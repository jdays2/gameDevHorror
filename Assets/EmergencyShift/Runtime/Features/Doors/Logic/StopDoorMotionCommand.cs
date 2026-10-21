using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Doors.Logic
{
    public readonly struct StopDoorMotionCommand : ICommand
    {
        private readonly DoorMotionModel model;
        public StopDoorMotionCommand(DoorMotionModel model) => this.model = model;
        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Missing door model.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Stop();
            }

            return result;
        }
    }
}
