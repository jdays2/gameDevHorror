using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Interaction.Config;
using EmergencyShift.Features.Interaction.Logic;

namespace EmergencyShift.Features.Interaction
{
    public sealed class PlayerInteractionControllerFactory
    {
        private readonly IGameClock clock;
        public PlayerInteractionControllerFactory(IGameClock clock) => this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        public InteractionSelectionController Create(PlayerInteractionSettings settings, IInteractionEnvironment environment) =>
            new InteractionSelectionController(new InteractionSelectionModel(settings), environment);
        public PickupNoticeModel CreateNotices(PlayerInteractionSettings settings) => new PickupNoticeModel(settings, clock);
    }
}
