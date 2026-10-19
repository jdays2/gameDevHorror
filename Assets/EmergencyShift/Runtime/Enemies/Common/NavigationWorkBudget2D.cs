using EmergencyShift.Core.Scheduling;
using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    /// <summary>Compatibility boundary for the existing shared path-build quota.</summary>
    public sealed class NavigationWorkBudget2D
    {
        public const int MaximumBuildsPerFrame = 2;
        private readonly FrameWorkBudget budget;

        public int TotalBuilds => budget.TotalReservations;
        public int PeakBuilds => budget.PeakReservations;

        public NavigationWorkBudget2D(IFrameClock clock)
        {
            budget = new FrameWorkBudget(clock, MaximumBuildsPerFrame);
        }

        public bool TryReserve() => budget.TryReserve();
    }
}
