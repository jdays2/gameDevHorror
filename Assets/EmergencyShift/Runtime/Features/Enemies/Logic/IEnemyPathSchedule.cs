namespace EmergencyShift.Features.Enemies.Logic
{
    public interface IEnemyPathSchedule
    {
        bool TryBeginBuild();
        void CompleteBuild();
        void Invalidate();
    }
}
