using System;

namespace EmergencyShift.Core.Randomness
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int NextInt(int minimumInclusive, int maximumExclusive) => random.Next(minimumInclusive, maximumExclusive);
        public double NextUnit() => random.NextDouble();
    }
}
