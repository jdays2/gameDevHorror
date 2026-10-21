using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class InterruptEnemyMeleeCommand : ICommand
    {
        private readonly EnemyMeleeModel model;
        private readonly float delay;

        public InterruptEnemyMeleeCommand(EnemyMeleeModel model, float delay)
        {
            this.model = model;
            this.delay = delay;
        }

        public CommandResult CanExecute() => model != null && model.ValidTime && float.IsFinite(delay) && delay >= 0
            ? CommandResult.Ok() : CommandResult.Fail("Melee interrupt needs finite nonnegative delay/time.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Interrupt(delay);
            }

            return result;
        }
    }
}
