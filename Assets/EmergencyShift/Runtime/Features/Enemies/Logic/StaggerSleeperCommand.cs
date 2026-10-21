using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class StaggerSleeperCommand : ICommand
    {
        private readonly SleeperModel model;
        private readonly Vector2 direction;
        public StaggerSleeperCommand(SleeperModel model, Vector2 direction)
        {
            this.model = model;
            this.direction = direction;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.IsAlive && SleeperModel.Finite(direction)
            ? CommandResult.Ok() : CommandResult.Fail("Sleeper action rejected: StaggerSleeper.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Stagger(direction);
            }

            return result;
        }
    }
}
