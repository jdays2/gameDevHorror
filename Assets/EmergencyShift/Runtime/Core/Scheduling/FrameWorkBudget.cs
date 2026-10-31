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

        public bool TryReserve() => TryReserve(1);

        public bool TryReserve(int amount)
        {
            if (amount < 1 || amount > Maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }
            int currentFrame = clock.Frame;
            if (frame != currentFrame)
            {
                frame = currentFrame;
                count = 0;
            }

            if (amount > Maximum - count)
            {
                return false;
            }

            count += amount;
            TotalReservations += amount;
            PeakReservations = Math.Max(PeakReservations, count);
            return true;
        }
    }
}
