using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    public sealed class CheckpointSessionController
    {
        private readonly CheckpointSessionModel model;
        public bool IsSaving => model.IsSaving;
        public string LastCheckpointId => model.LastCheckpointId;

        public CheckpointSessionController(CheckpointSessionModel model) =>
            this.model = model ?? throw new ArgumentNullException(nameof(model));

        public CommandResult CanRequest(string id, bool canSave) => new RequestCheckpointSaveCommand(model, id, canSave).CanExecute();
        public CommandResult Request(string id, bool canSave) => new RequestCheckpointSaveCommand(model, id, canSave).Execute();
        public void Cancel() => new ResetCheckpointSessionCommand(model, false).Execute();
        public void Clear() => new ResetCheckpointSessionCommand(model, true).Execute();
        public CommandResult RecordSaved(string id) => new RecordCheckpointSaveCommand(model, id).Execute();
        public CommandResult Restore(CheckpointVisitSnapshot snapshot) => new RestoreCheckpointSessionCommand(model, snapshot).Execute();
        public string[] CaptureVisits(string additionalId) => model.CaptureVisits(additionalId);
    }
}
