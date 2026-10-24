using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class SetStalkerScriptedControlCommand : ICommand
    {
        private readonly StalkerModel model;
        private readonly bool active;

        public SetStalkerScriptedControlCommand(StalkerModel model, bool active)
        {
            this.model = model;
            this.active = active;
        }

        public CommandResult CanExecute() => model != null && model.ValidClock && model.IsAlive && !model.HasAggro && model.IsScripted != active
            ? CommandResult.Ok() : CommandResult.Fail("Scripted control requires a living, non-aggressive Stalker and an ownership change.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.SetScriptedControl(active);
            }

            return result;
        }
    }
}
