using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class CompleteSleeperAttackCommand : ICommand
    {
        private readonly SleeperModel model;
        
        public CompleteSleeperAttackCommand(SleeperModel model)
        {
            this.model = model;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.State == SleeperState.Attacking && model.AttackProgress >= 1
            ? CommandResult.Ok() : CommandResult.Fail("Sleeper action rejected: CompleteSleeperAttack.");
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
