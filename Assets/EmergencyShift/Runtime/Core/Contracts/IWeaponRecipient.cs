namespace EmergencyShift
{
    public interface IWeaponRecipient : IInteractionActor
    {
        bool CanCollectWeapon { get; }
        bool HasWeapon { get; }
        int Ammo { get; }
        int MaximumAmmo { get; }
        string WeaponName { get; }
        int CollectAmmo(int amount, WeaponDefinition definition = null);
        bool CollectWeapon(int amount, WeaponDefinition definition);
    }
}
