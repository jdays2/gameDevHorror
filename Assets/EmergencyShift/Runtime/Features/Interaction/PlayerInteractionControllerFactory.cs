using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Interaction.Config;
using EmergencyShift.Features.Interaction.Logic;
using EmergencyShift.Features.PlayerRules.Config;

namespace EmergencyShift.Features.Interaction
{
    public sealed class PlayerInteractionControllerFactory
    {
        private readonly IGameClock clock;
        private readonly PlayerRulesSettings rules;

        public PlayerInteractionControllerFactory(IGameClock clock, PlayerRulesSettings rules = null)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.rules = rules ?? PlayerRulesSettings.LegacyDefaults;
        }

        public InteractionSelectionController Create(PlayerInteractionSettings settings, IInteractionEnvironment environment, bool forceAuthored = false) =>
            new InteractionSelectionController(new InteractionSelectionModel(rules.ProjectInteraction(settings, forceAuthored)), environment);
        public PickupNoticeModel CreateNotices(PlayerInteractionSettings settings, bool forceAuthored = false) =>
            new PickupNoticeModel(rules.ProjectInteraction(settings, forceAuthored), clock);
    }
}
