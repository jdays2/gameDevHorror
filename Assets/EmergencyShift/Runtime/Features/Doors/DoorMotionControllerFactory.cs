using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Doors.Config;
using EmergencyShift.Features.Doors.Logic;

namespace EmergencyShift.Features.Doors
{
    public sealed class DoorMotionControllerFactory
    {
        private readonly IGameClock clock;
        public DoorMotionControllerFactory(IGameClock clock) => this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        public DoorMotionController Create(float duration, IDoorMotionView view) =>
            new DoorMotionController(new DoorMotionModel(new DoorMotionSettings(duration), clock), view);
    }
}
