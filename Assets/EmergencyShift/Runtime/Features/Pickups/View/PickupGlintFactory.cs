using System;
using EmergencyShift.Features.Pickups.Config;
using UnityEngine;

namespace EmergencyShift.Features.Pickups.View
{
    /// <summary>Level-owned native presentation attachment; scenes and prefab assets remain authored.</summary>
    public sealed class PickupGlintFactory
    {
        private readonly PickupGlintProfile profile;
        private readonly Flashlight2D flashlight;
        private readonly PlayerInteraction actor;

        public PickupGlintFactory(PickupGlintProfile profile, Flashlight2D flashlight, PlayerLife2D player)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.flashlight = flashlight;
            actor = player != null ? player.GetComponent<PlayerInteraction>() : null;
        }

        public void Attach(MonoBehaviour target)
        {
            if (target.GetComponent<PickupGlint2D>() != null)
            {
                throw new InvalidOperationException("Pickup glint must be composed once per instance.");
            }

            var view = target.gameObject.AddComponent<PickupGlint2D>();
            view.Configure(profile, flashlight, actor, target);
        }
    }
}
