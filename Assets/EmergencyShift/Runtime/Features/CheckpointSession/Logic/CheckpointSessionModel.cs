using System;
using System.Collections.Generic;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    /// <summary>Personal save-request and visit history. IO, coroutines and scenes belong to adapters.</summary>
    public sealed class CheckpointSessionModel
    {
        private readonly HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        public string PendingId { get; private set; }
        public string LastCheckpointId { get; private set; }
        public bool IsSaving => PendingId != null;
        public int VisitedCount => visited.Count;
        public bool HasVisited(string id) => visited.Contains(id);

        public string[] CaptureVisits(string additionalId)
        {
            if (string.IsNullOrWhiteSpace(additionalId))
            {
                throw new ArgumentException("Checkpoint ID is required.", nameof(additionalId));
            }

            bool add = !visited.Contains(additionalId);
            var values = new string[visited.Count + (add ? 1 : 0)];
            visited.CopyTo(values);
            if (add)
            {
                values[values.Length - 1] = additionalId;
            }

            return values;
        }

        internal void Request(string id) => PendingId = id;

        internal void Reset(bool clearHistory)
        {
            PendingId = null;
            if (clearHistory)
            {
                visited.Clear();
                LastCheckpointId = null;
            }
        }

        internal void RecordSaved(string id)
        {
            visited.Add(id);
            LastCheckpointId = id;
            if (PendingId == id)
            {
                PendingId = null;
            }
        }

        internal void Restore(CheckpointVisitSnapshot snapshot)
        {
            PendingId = null;
            visited.Clear();
            for (int index = 0; index < snapshot.Count; index++)
            {
                visited.Add(snapshot.At(index));
            }

            RecordSaved(snapshot.CheckpointId);
        }
    }
}
