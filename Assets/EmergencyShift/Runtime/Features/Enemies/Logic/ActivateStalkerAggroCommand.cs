using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class ActivateStalkerAggroCommand : ICommand
    {
        private readonly StalkerModel model;
        public ActivateStalkerAggroCommand(StalkerModel model)
        {
            this.model = model;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.IsAlive && !model.HasAggro
            ? CommandResult.Ok() : CommandResult.Fail("Stalker action rejected: ActivateStalkerAggro.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.ActivateAggro();
            }

            return result;
        }
    }
}
