using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Health.Logic
{
    public readonly struct SetHealthCapacityCommand : ICommand
    {
        private readonly HealthModel target;
        private readonly int maximum;
        private readonly int current;

        public SetHealthCapacityCommand(HealthModel target, int maximum, int current)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.maximum = maximum;
            this.current = current;
        }

        public CommandResult CanExecute()
        {
            return target.IsAlive ? CommandResult.Ok() : CommandResult.Fail("Capacity cannot revive a dead target.");
        }

        public CommandResult Execute()
        {
            CommandResult validation = CanExecute();
            if (!validation.Success)
            {
                return validation;
            }

            target.SetCapacity(maximum, current);
            return CommandResult.Ok();
        }
    }
}
