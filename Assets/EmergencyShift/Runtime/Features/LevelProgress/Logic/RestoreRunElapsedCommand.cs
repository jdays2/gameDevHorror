using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.LevelProgress.Logic
{
    public readonly struct RestoreRunElapsedCommand : ICommand
    {
        private readonly LevelRunModel model;
        private readonly float seconds;

        public RestoreRunElapsedCommand(LevelRunModel model, float seconds)
        {
            this.model = model;
            this.seconds = seconds;
        }

        public CommandResult CanExecute() => model != null && float.IsFinite(seconds)
            ? CommandResult.Ok() : CommandResult.Fail("Elapsed time requires a model and a finite value.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.RestoreElapsed(seconds);
            }

            return result;
        }
    }
}
