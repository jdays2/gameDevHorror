using System;
using EmergencyShift.Features.PlayerInput.Logic;
using EmergencyShift.Features.PlayerMovement.Config;
using EmergencyShift.Features.PlayerMovement.Logic;
using EmergencyShift.Features.PlayerRules.Config;

namespace EmergencyShift.Features.PlayerMovement
{
    public sealed class PlayerMovementControllerFactory
    {
        private readonly IPlayerInput input;
        private readonly GameplayInputBlock inputBlock;
        private readonly PlayerRulesSettings rules;
        public PlayerMovementControllerFactory(IPlayerInput input, GameplayInputBlock inputBlock, PlayerRulesSettings rules = null)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.inputBlock = inputBlock ?? throw new ArgumentNullException(nameof(inputBlock));
            this.rules = rules ?? PlayerRulesSettings.LegacyDefaults;
        }

        public PlayerMovementController Create(float authoredSpeed, bool forceAuthored = false) =>
            new PlayerMovementController(new PlayerMovementModel(new PlayerMovementSettings(rules.ProjectMoveSpeed(authoredSpeed, forceAuthored))), input, inputBlock);
    }
}
