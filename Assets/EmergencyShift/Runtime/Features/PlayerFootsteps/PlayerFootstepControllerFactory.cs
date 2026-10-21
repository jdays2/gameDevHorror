using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.PlayerFootsteps.Config;
using EmergencyShift.Features.PlayerFootsteps.Logic;

namespace EmergencyShift.Features.PlayerFootsteps
{
    public sealed class PlayerFootstepControllerFactory
    {
        private readonly IGameClock clock;
        public PlayerFootstepControllerFactory(IGameClock clock) => this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        public PlayerFootstepController Create(PlayerFootstepSettings settings, IPlayerFootstepView view) =>
            new PlayerFootstepController(new PlayerFootstepModel(settings, clock), view);
    }
}
