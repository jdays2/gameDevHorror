using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class RestoreStalkerCommand : ICommand
    {
        private readonly StalkerModel model;
        private readonly StalkerStateSnapshot state;
        public RestoreStalkerCommand(StalkerModel model, StalkerStateSnapshot state)
        {
            this.model = model;
            this.state = state;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && StalkerModel.ValidateSnapshot(state).Success
            ? CommandResult.Ok() : CommandResult.Fail("Stalker action rejected: RestoreStalker.");
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
