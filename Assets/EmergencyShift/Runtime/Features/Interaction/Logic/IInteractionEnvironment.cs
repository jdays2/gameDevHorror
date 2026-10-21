namespace EmergencyShift.Features.Interaction.Logic
{
    public interface IInteractionEnvironment
    {
        bool CanAct { get; }
        int ScanCandidates();
        InteractionCandidate Observe(int index);
        bool IsAvailable(int index);
        bool Interact(int index);
        void PresentTarget(int index);
    }
}
