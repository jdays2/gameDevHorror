using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Health.Config;

namespace EmergencyShift.Features.Health.Logic
{
    /// <summary>The world clock is shared; each call creates an independent actor model.</summary>
    public sealed class HealthModelFactory
    {
        private readonly IGameClock clock;

        public HealthModelFactory(IGameClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public HealthModel Create(HealthSettings settings) => new HealthModel(settings, clock);
    }
}
