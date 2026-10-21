using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class CheckDoorAccessCommand : ICommand
    {
        private readonly DoorAccessModel model;
        private readonly bool hasCard;
        private readonly bool hasNoticeRecipient;
        private readonly double now;

        public CheckDoorAccessCommand(DoorAccessModel model, bool hasCard, bool hasNoticeRecipient, double now)
        {
            this.model = model;
            this.hasCard = hasCard;
            this.hasNoticeRecipient = hasNoticeRecipient;
            this.now = now;
        }

        public DoorAccessAttempt Attempt { get; private set; }

        public CommandResult CanExecute() => model != null && double.IsFinite(now) && now >= 0
            ? CommandResult.Ok() : CommandResult.Fail("Access requires a reader and finite nonnegative time.");

        public CommandResult Execute()
        {
            Attempt = default;
            CommandResult result = CanExecute();
            if (result.Success)
            {
                Attempt = model.ApplyAccess(hasCard, hasNoticeRecipient, now);
            }

            return result;
        }
    }
}
