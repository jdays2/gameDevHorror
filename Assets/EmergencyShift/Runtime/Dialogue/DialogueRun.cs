using System;
using System.Collections.Generic;

namespace EmergencyShift
{
    public enum DialogueTransitionKind
    {
        Started,
        Advanced,
        ChoiceSelected,
        Completed,
        Cancelled
    }

    /// <summary>Stable content IDs for observers; contains no world-event implementation.</summary>
    public readonly struct DialogueTransition
    {
        public DialogueSequence Sequence { get; }
        public DialogueTransitionKind Kind { get; }
        public string FromId { get; }
        public string ToId { get; }
        public string ChoiceId { get; }

        public DialogueTransition(DialogueSequence sequence, DialogueTransitionKind kind, string fromId, string toId, string choiceId = null)
        {
            Sequence = sequence;
            Kind = kind;
            FromId = fromId;
            ToId = toId;
            ChoiceId = choiceId;
        }
    }

    /// <summary>One traversal of shared dialogue data. Independent from UI, input and game events.</summary>
    public sealed class DialogueRun
    {
        private readonly DialogueSequence sequence;
        private readonly Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);
        public int LineIndex { get; private set; }
        public bool IsComplete { get; private set; }
        public DialogueSequence.Line Current => IsComplete ? null : sequence.lines[LineIndex];
        public bool RequiresChoice => Current != null && Current.choices != null && Current.choices.Length > 0;
        public DialogueTransition LastTransition { get; private set; }

        public DialogueRun(DialogueSequence content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            content.Validate();
            sequence = content;
            for (int i = 0; i < content.lines.Length; i++)
            {
                indices.Add(content.lines[i].id, i);
            }

            LineIndex = indices[content.entryId];
            LastTransition = new DialogueTransition(sequence, DialogueTransitionKind.Started, null, Current.id);
        }

        public bool Advance()
        {
            if (IsComplete || RequiresChoice)
            {
                return false;
            }

            Move(Current.nextId, DialogueTransitionKind.Advanced);
            return true;
        }

        public bool Choose(int index)
        {
            if (!RequiresChoice || index < 0 || index >= Current.choices.Length)
            {
                return false;
            }

            var choice = Current.choices[index];
            Move(choice.targetId, DialogueTransitionKind.ChoiceSelected, choice.id);
            return true;
        }

        public void Cancel()
        {
            if (IsComplete)
            {
                return;
            }

            LastTransition = new DialogueTransition(sequence, DialogueTransitionKind.Cancelled, Current.id, null);
            IsComplete = true;
        }

        private void Move(string targetId, DialogueTransitionKind kind, string choiceId = null)
        {
            string from = Current.id;
            IsComplete = string.IsNullOrEmpty(targetId);
            if (IsComplete)
            {
                kind = DialogueTransitionKind.Completed;
            }
            else
            {
                LineIndex = indices[targetId];
            }

            LastTransition = new DialogueTransition(sequence, kind, from, targetId, choiceId);
        }
    }
}
