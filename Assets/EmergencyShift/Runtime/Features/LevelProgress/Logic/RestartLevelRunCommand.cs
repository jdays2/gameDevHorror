using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.LevelProgress.Logic
{
    public readonly struct RestartLevelRunCommand : ICommand
    {
        private readonly LevelRunModel model;
        private readonly ILevelRunEnvironment environment;

        public RestartLevelRunCommand(LevelRunModel model, ILevelRunEnvironment environment)
        {
            this.model = model;
            this.environment = environment;
        }

        public CommandResult CanExecute() => model != null && environment != null && model.CanRestart
            ? CommandResult.Ok() : CommandResult.Fail("Only a completed run may restart without reentry.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }

            model.BeginRestart();
            try
            {
                environment.RestartScene();
            }
            finally
            {
                model.EndRestart();
            }

            return result;
        }
    }
}
