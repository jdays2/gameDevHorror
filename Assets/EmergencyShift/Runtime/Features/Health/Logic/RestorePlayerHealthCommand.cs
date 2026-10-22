using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Health.Logic
{
    /// <summary>Trusted player restoration retains the previous live-health clamp and clears protection.</summary>
    public readonly struct RestorePlayerHealthCommand : ICommand
    {
        private readonly HealthModel model;
        private readonly int current;

        public RestorePlayerHealthCommand(HealthModel model, int current)
        {
            this.model = model;
            this.current = current;
        }

        public CommandResult CanExecute() => model != null
            ? CommandResult.Ok() : CommandResult.Fail("Player health model is missing.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Restore(Math.Max(1, Math.Min(current, model.Maximum)));
            }

            return result;
        }
    }
}
