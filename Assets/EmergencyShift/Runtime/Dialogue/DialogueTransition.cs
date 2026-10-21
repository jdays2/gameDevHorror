namespace EmergencyShift
{
    /// <summary>Presentation notification retaining the asset for existing scene observers.</summary>
    public readonly struct DialogueTransition
    {
        public DialogueSequence Sequence { get; }
        public DialogueTransitionKind Kind { get; }
        public string FromId { get; }
        public string ToId { get; }
        public string ChoiceId { get; }

        public DialogueTransition(DialogueSequence sequence, DialogueStep step)
        {
            Sequence = sequence;
            Kind = step.Kind;
            FromId = step.FromId;
            ToId = step.ToId;
            ChoiceId = step.ChoiceId;
        }
    }
}
