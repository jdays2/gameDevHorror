using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.DeathScreen.Logic
{
    public readonly struct DeathScreenCommand : ICommand
    {
        private readonly DeathScreenController controller;
        private readonly DeathScreenIntent intent;

        public DeathScreenCommand(DeathScreenController controller, DeathScreenIntent intent)
        {
            this.controller = controller;
            this.intent = intent;
        }

        public CommandResult CanExecute() => controller == null
            ? CommandResult.Fail("A death screen scope is required.") : controller.Validate(intent);

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                controller.Apply(intent);
            }
            return result;
        }
    }
}
