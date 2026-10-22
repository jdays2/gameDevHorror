namespace EmergencyShift.Features.Health.Logic
{
    public interface IPlayerLifeEnvironment
    {
        void SetDefeated(bool defeated);
        void RestartRoom();
    }
}
