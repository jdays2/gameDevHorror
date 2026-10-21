using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class RestoreEnemyMeleeCommand : ICommand
    {
        private readonly EnemyMeleeModel model;
        private readonly EnemyMeleeState state;

        public RestoreEnemyMeleeCommand(EnemyMeleeModel model, EnemyMeleeState state)
        {
            this.model = model;
            this.state = state;
        }

        public CommandResult CanExecute() => model == null || !model.ValidTime
            ? CommandResult.Fail("Melee needs a valid model clock.") : EnemyMeleeModel.ValidateSnapshot(state);

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
