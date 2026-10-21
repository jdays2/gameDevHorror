using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AccessControl.Config;
using EmergencyShift.Features.AccessControl.Logic;

namespace EmergencyShift.Features.AccessControl
{
    public sealed class DoorAccessControllerFactory
    {
        private readonly IGameClock clock;

        public DoorAccessControllerFactory(IGameClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public DoorAccessController Create(DoorAccessSettings settings)
        {
            return new DoorAccessController(new DoorAccessModel(settings), clock);
        }
    }
}
