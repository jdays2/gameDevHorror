namespace EmergencyShift
{
    /// <summary>Native inventory queries. Inspecting a pickup never applies its effect.</summary>
    public interface IPickupPreviewActor
    {
        bool CanAcceptFlashlight { get; }
        bool CanAcceptCard(string id);
        bool CanAcceptWeapon(WeaponDefinition definition);
        bool CanAcceptAmmo(WeaponDefinition definition);
        bool CanAcceptModule(string id, float rangeBonus, float angleBonus, bool repeatable);
    }
}
