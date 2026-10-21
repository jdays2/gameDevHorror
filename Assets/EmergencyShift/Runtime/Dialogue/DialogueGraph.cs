using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmergencyShift
{
    /// <summary>Validated immutable content snapshot, without Unity object references.</summary>
    public sealed class DialogueGraph
    {
        private readonly Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);
        public ReadOnlyCollection<DialogueNode> Nodes { get; }
        public int EntryIndex { get; }

        public DialogueGraph(string entryId, int speakerCount, IEnumerable<DialogueNode> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            var copy = new List<DialogueNode>(nodes);
            if (speakerCount <= 0 || copy.Count == 0)
            {
                throw new InvalidOperationException("Dialogue requires speakers and lines.");
            }

            for (int i = 0; i < copy.Count; i++)
            {
                DialogueNode node = copy[i];
                if (node == null || node.Speaker < 0 || node.Speaker >= speakerCount
                    || string.IsNullOrWhiteSpace(node.Text) || string.IsNullOrWhiteSpace(node.Id)
                    || indices.ContainsKey(node.Id))
                {
                    throw new InvalidOperationException("Dialogue line has an invalid speaker, text or ID.");
                }

                indices.Add(node.Id, i);
            }

            if (string.IsNullOrWhiteSpace(entryId) || !indices.TryGetValue(entryId, out int entry))
            {
                throw new InvalidOperationException("Dialogue entry does not exist.");
            }

            foreach (DialogueNode node in copy)
            {
                if (node.Choices.Count > 0 && !string.IsNullOrEmpty(node.NextId))
                {
                    throw new InvalidOperationException("A line cannot have both nextId and choices: " + node.Id);
                }

                if (!string.IsNullOrEmpty(node.NextId) && !indices.ContainsKey(node.NextId))
                {
                    throw new InvalidOperationException("Dialogue next target does not exist: " + node.Id);
                }

                var choiceIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (DialogueOption choice in node.Choices)
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.Id) || !choiceIds.Add(choice.Id)
                        || string.IsNullOrWhiteSpace(choice.Text) || string.IsNullOrWhiteSpace(choice.TargetId)
                        || !indices.ContainsKey(choice.TargetId))
                    {
                        throw new InvalidOperationException("Dialogue choice has an invalid ID, text or target: " + node.Id);
                    }
                }
            }

            EntryIndex = entry;
            Nodes = copy.AsReadOnly();
        }

        internal int IndexOf(string id) => indices[id];
    }
}
