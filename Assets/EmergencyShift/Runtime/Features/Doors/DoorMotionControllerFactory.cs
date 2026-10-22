using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Doors.Config;
using EmergencyShift.Features.Doors.Logic;

namespace EmergencyShift.Features.Doors
{
    public sealed class DoorMotionControllerFactory
    {
        private readonly IGameClock clock;
        private readonly DoorRulesSettings rules;
        public float InteractionRange => rules.InteractionRange;

        public DoorMotionControllerFactory(IGameClock clock, DoorRulesSettings rules = null)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.rules = rules ?? DoorRulesSettings.LegacyDefaults;
        }

        public DoorMotionController Create(float duration, IDoorMotionView view, bool forceAuthored = false) =>
            new DoorMotionController(new DoorMotionModel(new DoorMotionSettings(rules.ProjectDuration(duration, forceAuthored)), clock), view);
    }
}
