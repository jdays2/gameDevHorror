using System;
using System.Collections.Generic;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    public sealed class CheckpointVisitSnapshot
    {
        private readonly string[] visits;
        public string CheckpointId { get; }
        public int Count => visits.Length;
        public string At(int index) => visits[index];

        public CheckpointVisitSnapshot(string checkpointId, string[] visited)
        {
            if (string.IsNullOrWhiteSpace(checkpointId))
            {
                throw new ArgumentException("Checkpoint ID is required.", nameof(checkpointId));
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (visited != null)
            {
                foreach (string id in visited)
                {
                    if (string.IsNullOrWhiteSpace(id) || !unique.Add(id))
                    {
                        throw new ArgumentException("Invalid visited checkpoint IDs.", nameof(visited));
                    }
                }
            }

            CheckpointId = checkpointId;
            visits = visited != null ? (string[])visited.Clone() : Array.Empty<string>();
        }
    }
}
