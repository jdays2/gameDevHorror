using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class CompleteStalkerAttackCommand : ICommand
    {
        private readonly StalkerModel model;
        public CompleteStalkerAttackCommand(StalkerModel model)
        {
            this.model = model;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.State == StalkerState.Attacking && model.AttackProgress >= 1
            ? CommandResult.Ok() : CommandResult.Fail("Stalker action rejected: CompleteStalkerAttack.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.CompleteAttack();
            }

            return result;
        }
    }
}
