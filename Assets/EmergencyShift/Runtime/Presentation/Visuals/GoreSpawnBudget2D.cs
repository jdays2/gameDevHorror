using EmergencyShift.Core.Scheduling;
using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    /// <summary>Existing fragment-construction quota; it never removes or rejects permanent remains.</summary>
    public sealed class GoreSpawnBudget2D
    {
        public const int MaximumFragmentsPerFrame = 32;
        private readonly FrameWorkBudget budget;

        public int PeakFragments => budget.PeakReservations;

        public GoreSpawnBudget2D(IFrameClock clock)
        {
            budget = new FrameWorkBudget(clock, MaximumFragmentsPerFrame);
        }

        public bool TryReserve() => budget.TryReserve();
    }
}
