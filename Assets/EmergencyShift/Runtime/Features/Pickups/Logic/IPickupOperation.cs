namespace EmergencyShift.Features.Pickups.Logic
{
    /// <summary>Native authorization/inventory effect and feedback, called in separate controller stages.</summary>
    public interface IPickupOperation
    {
        bool CanApply { get; }
        bool Apply();
        void Present();
    }
}
