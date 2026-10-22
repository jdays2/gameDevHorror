using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Health.Logic
{
    public readonly struct RestartPlayerRoomCommand : ICommand
    {
        private readonly HealthModel model;
        private readonly IPlayerLifeEnvironment environment;

        public RestartPlayerRoomCommand(HealthModel model, IPlayerLifeEnvironment environment)
        {
            this.model = model;
            this.environment = environment;
        }

        public CommandResult CanExecute() => model != null && environment != null && !model.IsAlive
            ? CommandResult.Ok() : CommandResult.Fail("Restart requires a defeated player and its room environment.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                environment.RestartRoom();
            }

            return result;
        }
    }
}
