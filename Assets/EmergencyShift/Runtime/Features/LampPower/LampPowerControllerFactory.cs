using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.LampPower.Logic;
using EmergencyShift.Features.LampPower.Config;

namespace EmergencyShift.Features.LampPower
{
    public sealed class LampPowerControllerFactory
    {
        private readonly IGameClock clock;

        public LampFlickerSettings Flicker { get; }

        public LampPowerControllerFactory(IGameClock clock, LampFlickerSettings flicker = null)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Flicker = flicker ?? LampFlickerSettings.LegacyDefaults;
        }

        public LampPowerController Create(ILampPowerView view, bool powered) => new LampPowerController(new LampPowerModel(clock, powered), view);
    }
}
