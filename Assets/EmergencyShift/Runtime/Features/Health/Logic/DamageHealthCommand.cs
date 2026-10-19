using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Health.Logic
{
    public readonly struct DamageHealthCommand : ICommand
    {
        private readonly HealthModel target;
        private readonly int amount;

        public DamageHealthCommand(HealthModel target, int amount)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.amount = amount;
        }

        public CommandResult CanExecute() => target.CanTakeDamage(amount);

        public CommandResult Execute()
        {
            CommandResult validation = CanExecute();
            if (!validation.Success)
            {
                return validation;
            }

            target.ApplyDamage(amount);
            return CommandResult.Ok();
        }
    }
}
