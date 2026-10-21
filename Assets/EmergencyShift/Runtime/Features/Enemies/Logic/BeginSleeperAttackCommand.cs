using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class BeginSleeperAttackCommand : ICommand
    {
        private readonly SleeperModel model;
        private readonly Vector2 heading;
        public BeginSleeperAttackCommand(SleeperModel model, Vector2 heading)
        {
            this.model = model;
            this.heading = heading;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.State == SleeperState.Hunting && SleeperModel.Finite(heading)
            ? CommandResult.Ok() : CommandResult.Fail("Sleeper action rejected: BeginSleeperAttack.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.BeginAttack(heading);
            }

            return result;
        }
    }
}
