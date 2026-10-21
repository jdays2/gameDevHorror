using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmergencyShift
{
    public sealed class DialogueNode
    {
        public string Id { get; }
        public int Speaker { get; }
        public string Text { get; }
        public string NextId { get; }
        public ReadOnlyCollection<DialogueOption> Choices { get; }

        public DialogueNode(string id, int speaker, string text, string nextId, IEnumerable<DialogueOption> choices = null)
        {
            Id = id;
            Speaker = speaker;
            Text = text;
            NextId = nextId;
            Choices = new List<DialogueOption>(choices ?? Array.Empty<DialogueOption>()).AsReadOnly();
        }
    }
}
