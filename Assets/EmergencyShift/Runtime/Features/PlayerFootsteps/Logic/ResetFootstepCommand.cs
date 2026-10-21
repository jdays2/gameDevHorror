using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.PlayerFootsteps.Logic
{
    public readonly struct ResetFootstepCommand : ICommand
    {
        private readonly PlayerFootstepModel model;
        private readonly Vector2 position;

        public ResetFootstepCommand(PlayerFootstepModel model, Vector2 position)
        {
            this.model = model;
            this.position = position;
        }

        public CommandResult CanExecute() => model != null && PlayerFootstepModel.ValidPosition(position)
            ? CommandResult.Ok() : CommandResult.Fail("Footstep reset requires a model and finite position.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Reset(position);
            }

            return result;
        }
    }
}
