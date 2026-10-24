using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.ScriptedEncounters.Logic
{
    public sealed class StartScriptedRouteCommand : ICommand
    {
        private readonly ScriptedRouteModel model;
        private readonly bool startSatisfied;

        public StartScriptedRouteCommand(ScriptedRouteModel model, bool startSatisfied)
        {
            this.model = model;
            this.startSatisfied = startSatisfied;
        }

        public CommandResult CanExecute() => model != null && model.State == ScriptedRouteState.WaitingForTrigger && startSatisfied
            ? CommandResult.Ok() : CommandResult.Fail("Scripted route needs its first satisfied start condition.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Start();
            }

            return result;
        }
    }
}
