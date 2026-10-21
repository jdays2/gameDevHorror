using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class RestoreEnemyPatrolCommand : ICommand
    {
        private readonly EnemyPatrolModel model;
        private readonly EnemyPatrolState state;
        public RestoreEnemyPatrolCommand(EnemyPatrolModel model, EnemyPatrolState state)
        {
            this.model = model;
            this.state = state;
        }

        public CommandResult CanExecute() => model == null ? CommandResult.Fail("Missing patrol model.") : EnemyPatrolModel.ValidateSnapshot(state);
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(state);
            }

            return result;
        }
    }
}
