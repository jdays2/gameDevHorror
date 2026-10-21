using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.PlayerFootsteps.Logic
{
    public sealed class PlayerFootstepController
    {
        private readonly PlayerFootstepModel model;
        private readonly IPlayerFootstepView view;

        public PlayerFootstepController(PlayerFootstepModel model, IPlayerFootstepView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Sample(Vector2 position, float velocitySquared, bool canTravel)
        {
            if (model.Tick(position, velocitySquared, canTravel))
            {
                view.PlayStep(position);
            }
        }

        public CommandResult Reset(Vector2 position) => new ResetFootstepCommand(model, position).Execute();
    }
}
