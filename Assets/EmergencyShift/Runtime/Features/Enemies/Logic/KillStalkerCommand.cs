using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class KillStalkerCommand : ICommand
    {
        private readonly StalkerModel model;
        public KillStalkerCommand(StalkerModel model)
        {
            this.model = model;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.IsAlive
            ? CommandResult.Ok() : CommandResult.Fail("Stalker action rejected: KillStalker.");
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
