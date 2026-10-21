namespace EmergencyShift.Features.LevelObjectives.Logic
{
    public interface IObjectiveFacts
    {
        bool HasWeapon { get; }
        bool HasCard(string id);
    }
}
