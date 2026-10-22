using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.LevelProgress.Logic
{
    public readonly struct FinishLevelRunCommand : ICommand
    {
        private readonly LevelRunModel model;
        private readonly bool ownsActor;
        private readonly bool alive;
        private readonly bool hasExitAccess;

        public FinishLevelRunCommand(LevelRunModel model, bool ownsActor, bool alive, bool hasExitAccess)
        {
            this.model = model;
            this.ownsActor = ownsActor;
            this.alive = alive;
            this.hasExitAccess = hasExitAccess;
        }

        public CommandResult CanExecute() => model != null ? model.CanFinish(ownsActor, alive, hasExitAccess)
            : CommandResult.Fail("Missing run model.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Finish();
            }

            return result;
        }
    }
}
