namespace EmergencyShift.Features.Encounter.Logic
{
    /// <summary>One-shot gameplay state. Restoration never starts an encounter.</summary>
    public sealed class EncounterModel
    {
        public bool Activated { get; private set; }

        public EncounterModel(bool activated = false) => Activated = activated;
        internal void Activate() => Activated = true;
        internal void Restore(bool activated) => Activated = activated;
    }
}
