using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Time;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>Personal replan timer; shared quota, clock and private RNG are three explicit dependencies.</summary>
    public sealed class EnemyPathSchedule : IEnemyPathSchedule
    {
        private const double RepathSeconds = .65;
        private const double JitterSeconds = .12;
        private readonly IGameClock clock;
        private readonly IEnemyPathBuildBudget budget;
        private readonly IRandomSource random;
        private double nextRepath;

        public EnemyPathSchedule(IGameClock clock, IEnemyPathBuildBudget budget, IRandomSource random)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.budget = budget ?? throw new ArgumentNullException(nameof(budget));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool TryBeginBuild()
        {
            double now = clock.Time;
            return double.IsFinite(now) && now >= 0 && now >= nextRepath && budget.TryReserve();
        }

        public void CompleteBuild()
        {
            double now = clock.Time;
            if (!double.IsFinite(now) || now < 0)
            {
                throw new InvalidOperationException("Navigation scheduling requires a finite nonnegative clock.");
            }

            double unit = random.NextUnit();
            if (!double.IsFinite(unit) || unit < 0 || unit > 1)
            {
                throw new InvalidOperationException("Navigation jitter requires a finite unit random value.");
            }

            nextRepath = now + RepathSeconds + unit * JitterSeconds;
        }

        public void Invalidate() => nextRepath = 0;
    }
}
