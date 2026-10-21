using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class BeginEnemyMeleeCommand : ICommand
    {
        private readonly EnemyMeleeModel model;
        public BeginEnemyMeleeCommand(EnemyMeleeModel model) => this.model = model;
        public CommandResult CanExecute() => model != null && model.ValidTime
            ? CommandResult.Ok() : CommandResult.Fail("Melee needs a valid model clock.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Begin();
            }

            return result;
        }
    }
}
