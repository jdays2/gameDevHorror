namespace EmergencyShift
{
    public interface IWeaponUseGate
    {
        bool InputBlocked { get; }
        bool CanUseWeapon { get; }
    }
}
