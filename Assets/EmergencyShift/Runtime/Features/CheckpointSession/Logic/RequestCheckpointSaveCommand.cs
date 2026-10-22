using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    public readonly struct RequestCheckpointSaveCommand : ICommand
    {
        private readonly CheckpointSessionModel model;
        private readonly string id;
        private readonly bool canSave;

        public RequestCheckpointSaveCommand(CheckpointSessionModel model, string id, bool canSave)
        {
            this.model = model;
            this.id = id;
            this.canSave = canSave;
        }

        public CommandResult CanExecute() => model != null && canSave && !string.IsNullOrWhiteSpace(id) && !model.HasVisited(id)
            ? CommandResult.Ok() : CommandResult.Fail("Checkpoint request is unavailable or already visited.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Request(id);
            }

            return result;
        }
    }
}
