using System;

namespace EmergencyShift
{
    /// <summary>One pure traversal of an immutable graph. UI, input and clocks live outside the model.</summary>
    public sealed class DialogueRun
    {
        private readonly DialogueGraph graph;
        public int LineIndex { get; private set; }
        public bool IsComplete { get; private set; }
        public DialogueNode Current => IsComplete ? null : graph.Nodes[LineIndex];
        public bool RequiresChoice => Current != null && Current.Choices.Count > 0;
        public DialogueStep LastTransition { get; private set; }

        public DialogueRun(DialogueGraph content)
        {
            graph = content ?? throw new ArgumentNullException(nameof(content));
            LineIndex = graph.EntryIndex;
            LastTransition = new DialogueStep(DialogueTransitionKind.Started, null, Current.Id);
        }

        public bool Advance() => new AdvanceDialogueCommand(this).Execute().Success;

        public bool Choose(int index) => new ChooseDialogueCommand(this, index).Execute().Success;

        public void Cancel() => new CancelDialogueCommand(this).Execute();

        internal void ApplyAdvance() => Move(Current.NextId, DialogueTransitionKind.Advanced);

        internal void ApplyChoice(int index)
        {
            DialogueOption choice = Current.Choices[index];
            Move(choice.TargetId, DialogueTransitionKind.ChoiceSelected, choice.Id);
        }

        internal void ApplyCancel()
        {
            LastTransition = new DialogueStep(DialogueTransitionKind.Cancelled, Current.Id, null);
            IsComplete = true;
        }

        private void Move(string targetId, DialogueTransitionKind kind, string choiceId = null)
        {
            string from = Current.Id;
            IsComplete = string.IsNullOrEmpty(targetId);
            if (IsComplete)
            {
                kind = DialogueTransitionKind.Completed;
            }
            else
            {
                LineIndex = graph.IndexOf(targetId);
            }

            LastTransition = new DialogueStep(kind, from, targetId, choiceId);
        }
    }
}
