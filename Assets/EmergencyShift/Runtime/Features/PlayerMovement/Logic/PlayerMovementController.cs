using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine;

namespace EmergencyShift.Features.PlayerMovement.Logic
{
    /// <summary>Three explicit roles: personal movement rules, input intent and modal authorization.</summary>
    public sealed class PlayerMovementController
    {
        private readonly PlayerMovementModel model;
        private readonly IPlayerInput input;
        private readonly GameplayInputBlock inputBlock;

        public PlayerMovementController(PlayerMovementModel model, IPlayerInput input, GameplayInputBlock inputBlock)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.inputBlock = inputBlock ?? throw new ArgumentNullException(nameof(inputBlock));
        }

        // Recheck at the physical boundary: a modal can open between Update and FixedUpdate.
        public Vector2 Velocity => inputBlock.IsBlocked ? Vector2.zero : model.Velocity;
        public CommandResult SampleInput()
        {
            Vector2 intent = inputBlock.IsBlocked ? Vector2.zero : input.Movement;
            CommandResult result = new SetMovementIntentCommand(model, intent).Execute();
            if (!result.Success)
            {
                Stop();
            }

            return result;
        }

        public CommandResult Stop() => new SetMovementIntentCommand(model, Vector2.zero).Execute();
    }
}
