using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.LampPower.Logic;

namespace EmergencyShift.Features.LampPower
{
    public sealed class LampPowerControllerFactory
    {
        private readonly IGameClock clock;

        public LampPowerControllerFactory(IGameClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public LampPowerController Create(ILampPowerView view, bool powered) => new LampPowerController(new LampPowerModel(clock, powered), view);
    }
}
