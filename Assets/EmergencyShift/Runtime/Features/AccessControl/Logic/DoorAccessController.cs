using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AccessControl.Config;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class DoorAccessController
    {
        private readonly DoorAccessModel model;
        private readonly IGameClock clock;

        public DoorAccessController(DoorAccessModel model, IGameClock clock)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool IsUnlocked => model.IsUnlocked;

        public DoorAccessAttempt TryUnlock(bool hasCard, bool hasNoticeRecipient)
        {
            var command = new CheckDoorAccessCommand(model, hasCard, hasNoticeRecipient, clock.Time);
            command.Execute();
            return command.Attempt;
        }

        public void Restore(bool unlocked) => new RestoreDoorAccessCommand(model, unlocked).Execute();

        public void Configure(DoorAccessSettings settings) => new ConfigureDoorAccessCommand(model, settings).Execute();
    }
}
