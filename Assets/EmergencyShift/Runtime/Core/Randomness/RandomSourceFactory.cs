using System;

namespace EmergencyShift.Core.Randomness
{
    /// <summary>Composition owns seeding; consumers receive separate streams, never a global random state.</summary>
    public sealed class RandomSourceFactory
    {
        private readonly Random seeds;

        public RandomSourceFactory(Random seeds)
        {
            this.seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
        }

        public IRandomSource Create() => new SystemRandomSource(new Random(seeds.Next()));
    }
}
