using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    public readonly struct RestoreCheckpointSessionCommand : ICommand
    {
        private readonly CheckpointSessionModel model;
        private readonly CheckpointVisitSnapshot snapshot;

        public RestoreCheckpointSessionCommand(CheckpointSessionModel model, CheckpointVisitSnapshot snapshot)
        {
            this.model = model;
            this.snapshot = snapshot;
        }

        public CommandResult CanExecute() => model != null && snapshot != null
            ? CommandResult.Ok() : CommandResult.Fail("Checkpoint session and validated visits are required.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(snapshot);
            }

            return result;
        }
    }
}
