using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class BeginStalkerAttackCommand : ICommand
    {
        private readonly StalkerModel model;
        private readonly Vector2 heading;
        public BeginStalkerAttackCommand(StalkerModel model, Vector2 heading)
        {
            this.model = model;
            this.heading = heading;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.CanBeginAttack && StalkerModel.Finite(heading)
            ? CommandResult.Ok() : CommandResult.Fail("Stalker action rejected: BeginStalkerAttack.");
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
