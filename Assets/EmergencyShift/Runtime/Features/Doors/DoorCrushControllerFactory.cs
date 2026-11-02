using EmergencyShift.Features.Doors.Logic;

namespace EmergencyShift.Features.Doors
{
    public sealed class DoorCrushControllerFactory
    {
        public DoorCrushController Create(IDoorCrushEnvironment environment) =>
            new DoorCrushController(new DoorCrushModel(), environment);
    }
}
