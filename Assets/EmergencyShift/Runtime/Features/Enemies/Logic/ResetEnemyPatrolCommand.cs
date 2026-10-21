using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class ResetEnemyPatrolCommand : ICommand
    {
        private readonly EnemyPatrolModel model;
        public ResetEnemyPatrolCommand(EnemyPatrolModel model) => this.model = model;
        public CommandResult CanExecute() => model != null ? CommandResult.Ok() : CommandResult.Fail("Missing patrol model.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Reset();
            }

            return result;
        }
    }
}
