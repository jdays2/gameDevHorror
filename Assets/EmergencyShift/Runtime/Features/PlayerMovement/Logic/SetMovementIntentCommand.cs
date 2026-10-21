using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.PlayerMovement.Logic
{
    /// <summary>Value command: continuous input does not allocate a command object every frame.</summary>
    public readonly struct SetMovementIntentCommand : ICommand
    {
        private readonly PlayerMovementModel model;
        private readonly Vector2 intent;
        public SetMovementIntentCommand(PlayerMovementModel model, Vector2 intent)
        {
            this.model = model;
            this.intent = intent;
        }

        public CommandResult CanExecute() => model != null && PlayerMovementModel.ValidIntent(intent)
            ? CommandResult.Ok() : CommandResult.Fail("Movement requires a model and finite input.");
        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.SetIntent(intent);
            }

            return result;
        }
    }
}
