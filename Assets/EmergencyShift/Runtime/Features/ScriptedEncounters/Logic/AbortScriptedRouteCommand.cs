using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.ScriptedEncounters.Logic
{
    public sealed class AbortScriptedRouteCommand : ICommand
    {
        private readonly ScriptedRouteModel model;

        public AbortScriptedRouteCommand(ScriptedRouteModel model) => this.model = model;
        public CommandResult CanExecute() => model != null && model.ControlsActor
            ? CommandResult.Ok() : CommandResult.Fail("Scripted route already ended.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Abort();
            }

            return result;
        }
    }
}
