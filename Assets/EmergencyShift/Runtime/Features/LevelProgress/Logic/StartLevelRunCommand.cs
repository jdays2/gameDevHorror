using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.LevelProgress.Logic
{
    public readonly struct StartLevelRunCommand : ICommand
    {
        private readonly LevelRunModel model;
        public StartLevelRunCommand(LevelRunModel model) => this.model = model;
        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Missing run model.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Start();
            }

            return result;
        }
    }
}
