using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    public readonly struct ResetCheckpointSessionCommand : ICommand
    {
        private readonly CheckpointSessionModel model;
        private readonly bool clearHistory;

        public ResetCheckpointSessionCommand(CheckpointSessionModel model, bool clearHistory)
        {
            this.model = model;
            this.clearHistory = clearHistory;
        }

        public CommandResult CanExecute() => model != null
            ? CommandResult.Ok() : CommandResult.Fail("Checkpoint session is required.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Reset(clearHistory);
            }

            return result;
        }
    }
}
