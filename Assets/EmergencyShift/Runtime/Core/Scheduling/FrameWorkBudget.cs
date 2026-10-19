using System;
using EmergencyShift.Core.Time;

namespace EmergencyShift.Core.Scheduling
{
    /// <summary>Pure per-frame quota; one world can share it without sharing any actor state.</summary>
    public sealed class FrameWorkBudget
    {
        private readonly IFrameClock clock;
        private int frame = -1;
        private int count;

        public int Maximum { get; }
        public int TotalReservations { get; private set; }
        public int PeakReservations { get; private set; }

        public FrameWorkBudget(IFrameClock clock, int maximum)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (maximum < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            Maximum = maximum;
        }

        public bool TryReserve()
        {
            int currentFrame = clock.Frame;
            if (frame != currentFrame)
            {
                frame = currentFrame;
                count = 0;
            }

            if (count >= Maximum)
            {
                return false;
            }

            count++;
            TotalReservations++;
            PeakReservations = Math.Max(PeakReservations, count);
            return true;
        }
    }
}
