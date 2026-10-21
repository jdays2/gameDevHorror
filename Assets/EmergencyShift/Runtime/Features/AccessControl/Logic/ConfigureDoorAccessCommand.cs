using EmergencyShift.Core.Commands;
using EmergencyShift.Features.AccessControl.Config;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class ConfigureDoorAccessCommand : ICommand
    {
        private readonly DoorAccessModel model;
        private readonly DoorAccessSettings settings;

        public ConfigureDoorAccessCommand(DoorAccessModel model, DoorAccessSettings settings)
        {
            this.model = model;
            this.settings = settings;
        }

        public CommandResult CanExecute() => model != null && settings != null
            ? CommandResult.Ok() : CommandResult.Fail("Missing access reader or settings.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Configure(settings);
            }

            return result;
        }
    }
}
