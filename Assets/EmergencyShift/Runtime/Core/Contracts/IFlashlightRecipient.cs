namespace EmergencyShift
{
    public interface IFlashlightRecipient : IInteractionActor
    {
        bool CanCollectFlashlight { get; }
        bool CanUpgradeFlashlight { get; }
        bool TryCollectFlashlight();
        bool TryCollectUpgrade(string id, string label, float rangeBonus, float angleBonus, bool repeatable);
    }
}
