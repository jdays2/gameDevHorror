using System;
using EmergencyShift.Features.PlayerInput.Logic;
using EmergencyShift.Features.PlayerMovement.Config;
using EmergencyShift.Features.PlayerMovement.Logic;

namespace EmergencyShift.Features.PlayerMovement
{
    public sealed class PlayerMovementControllerFactory
    {
        private readonly IPlayerInput input;
        private readonly GameplayInputBlock inputBlock;
        public PlayerMovementControllerFactory(IPlayerInput input, GameplayInputBlock inputBlock)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.inputBlock = inputBlock ?? throw new ArgumentNullException(nameof(inputBlock));
        }

        public PlayerMovementController Create(float authoredSpeed) =>
            new PlayerMovementController(new PlayerMovementModel(new PlayerMovementSettings(authoredSpeed)), input, inputBlock);
    }
}
