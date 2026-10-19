using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    public readonly struct ShowDamagePulseCommand : ICommand
    {
        private readonly DirectionalDamageModel target;
        private readonly int amount;
        private readonly Vector2 towardSource;

        public ShowDamagePulseCommand(DirectionalDamageModel target, int amount, Vector2 towardSource)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.amount = amount;
            this.towardSource = towardSource;
        }

        public CommandResult CanExecute() => target.CanShow(amount, towardSource);

        public CommandResult Execute()
        {
            CommandResult validation = CanExecute();
            if (!validation.Success)
            {
                return validation;
            }

            target.Show(towardSource);
            return CommandResult.Ok();
        }
    }
}
