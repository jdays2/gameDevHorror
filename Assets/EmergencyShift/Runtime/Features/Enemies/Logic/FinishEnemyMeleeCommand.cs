using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class FinishEnemyMeleeCommand : ICommand
    {
        private readonly EnemyMeleeModel model;
        public FinishEnemyMeleeCommand(EnemyMeleeModel model) => this.model = model;
        public CommandResult CanExecute() => model != null && model.ValidTime
            ? CommandResult.Ok() : CommandResult.Fail("Melee needs a valid model clock.");

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
