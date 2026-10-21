using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class StaggerStalkerCommand : ICommand
    {
        private readonly StalkerModel model;
        private readonly Vector2 direction;
        private readonly bool targetAlive;
        public StaggerStalkerCommand(StalkerModel model, Vector2 direction, bool targetAlive)
        {
            this.model = model;
            this.direction = direction;
            this.targetAlive = targetAlive;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.IsAlive && StalkerModel.Finite(direction)
            ? CommandResult.Ok() : CommandResult.Fail("Stalker action rejected: StaggerStalker.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Stagger(direction, targetAlive);
            }

            return result;
        }
    }
}
