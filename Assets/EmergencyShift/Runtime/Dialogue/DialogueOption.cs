namespace EmergencyShift
{
    public sealed class DialogueOption
    {
        public string Id { get; }
        public string Text { get; }
        public string TargetId { get; }

        public DialogueOption(string id, string text, string targetId)
        {
            Id = id;
            Text = text;
            TargetId = targetId;
        }
    }
}
