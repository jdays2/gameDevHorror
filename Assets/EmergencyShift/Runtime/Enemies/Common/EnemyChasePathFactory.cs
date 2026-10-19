using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    public sealed class EnemyChasePathFactory
    {
        private readonly IGameClock clock;
        private readonly NavigationWorkBudget2D budget;
        private readonly RandomSourceFactory random;

        public EnemyChasePathFactory(IGameClock clock, NavigationWorkBudget2D budget, RandomSourceFactory random)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.budget = budget ?? throw new ArgumentNullException(nameof(budget));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        internal EnemyChasePath2D Create() => new EnemyChasePath2D(clock, budget, random.Create());
    }
}
