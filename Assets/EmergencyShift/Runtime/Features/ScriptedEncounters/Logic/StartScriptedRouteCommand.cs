using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.ScriptedEncounters.Logic
{
    public sealed class StartScriptedRouteCommand : ICommand
    {
        private readonly ScriptedRouteModel model;
        private readonly bool illuminated;

        public StartScriptedRouteCommand(ScriptedRouteModel model, bool illuminated)
        {
            this.model = model;
            this.illuminated = illuminated;
        }

        public CommandResult CanExecute() => model != null && model.State == ScriptedRouteState.WaitingForLight && illuminated
            ? CommandResult.Ok() : CommandResult.Fail("Scripted route needs its first real flashlight contact.");

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
