using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class KillSleeperCommand : ICommand
    {
        private readonly SleeperModel model;
        
        public KillSleeperCommand(SleeperModel model)
        {
            this.model = model;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.IsAlive
            ? CommandResult.Ok() : CommandResult.Fail("Sleeper action rejected: KillSleeper.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Die();
            }

            return result;
        }
    }
}
