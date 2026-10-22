using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    public readonly struct RecordCheckpointSaveCommand : ICommand
    {
        private readonly CheckpointSessionModel model;
        private readonly string id;

        public RecordCheckpointSaveCommand(CheckpointSessionModel model, string id)
        {
            this.model = model;
            this.id = id;
        }

        public CommandResult CanExecute() => model != null && !string.IsNullOrWhiteSpace(id)
            ? CommandResult.Ok() : CommandResult.Fail("Saved checkpoint ID is required.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.RecordSaved(id);
            }

            return result;
        }
    }
}
