using System;
using System.Collections.Generic;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AccessControl.Logic
{
    /// <summary>Access tokens owned by one actor; no scene, presentation or input dependency.</summary>
    public sealed class AccessCardModel
    {
        private readonly HashSet<string> cards = new HashSet<string>(StringComparer.Ordinal);

        public int Count => cards.Count;

        public bool Has(string id) => !string.IsNullOrWhiteSpace(id) && cards.Contains(id);

        public CommandResult CanAdd(string id)
        {
            return string.IsNullOrWhiteSpace(id) || cards.Contains(id)
                ? CommandResult.Fail("A nonempty, new card ID is required.")
                : CommandResult.Ok();
        }

        public string[] Capture()
        {
            var values = new string[cards.Count];
            cards.CopyTo(values);
            Array.Sort(values, StringComparer.Ordinal);
            return values;
        }

        public static CommandResult ValidateSnapshot(string[] values)
        {
            if (values == null)
            {
                // Legacy checkpoints omit an empty collection.
                return CommandResult.Ok();
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in values)
            {
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                {
                    return CommandResult.Fail("Missing or duplicate access card ID.");
                }
            }

            return CommandResult.Ok();
        }

        internal void Add(string id) => cards.Add(id);

        internal void Replace(string[] values)
        {
            cards.Clear();
            if (values != null)
            {
                foreach (string id in values)
                {
                    cards.Add(id);
                }
            }
        }
    }
}
