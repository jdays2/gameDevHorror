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

    /// <summary>Domain notification identified entirely by immutable content IDs.</summary>
    public readonly struct DialogueStep
    {
        public DialogueTransitionKind Kind { get; }
        public string FromId { get; }
        public string ToId { get; }
        public string ChoiceId { get; }

        public DialogueStep(DialogueTransitionKind kind, string fromId, string toId, string choiceId = null)
        {
            Kind = kind;
            FromId = fromId;
            ToId = toId;
            ChoiceId = choiceId;
        }
    }
}
