namespace EmergencyShift
{
    public interface IAccessCardRecipient : IInteractionActor
    {
        bool HasAccessCard(string id);
        bool TryCollectCard(string id);
    }
}
