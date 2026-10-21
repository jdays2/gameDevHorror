using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class RestoreSleeperCommand : ICommand
    {
        private readonly SleeperModel model;
        private readonly SleeperStateSnapshot state;
        public RestoreSleeperCommand(SleeperModel model, SleeperStateSnapshot state)
        {
            this.model = model;
            this.state = state;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && SleeperModel.ValidateSnapshot(state).Success
            ? CommandResult.Ok() : CommandResult.Fail("Sleeper action rejected: RestoreSleeper.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(state);
            }

            return result;
        }
    }
}
