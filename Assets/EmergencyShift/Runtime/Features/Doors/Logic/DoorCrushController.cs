using System;

namespace EmergencyShift.Features.Doors.Logic
{
    public sealed class DoorCrushController
    {
        private readonly DoorCrushModel model;
        private readonly IDoorCrushEnvironment environment;

        public DoorCrushController(DoorCrushModel model, IDoorCrushEnvironment environment)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public void Step(DoorLeafStep step)
        {
            if (model.CanCrush(step))
            {
                environment.CrushContacts(step.Displacement);
            }
        }
    }
}
