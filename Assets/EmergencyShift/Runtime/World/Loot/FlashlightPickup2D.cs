using System;
using EmergencyShift.Features.Pickups;
using EmergencyShift.Features.Pickups.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>One scene-authored flashlight; proximity and wall checks use the common pickup contract.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CircleCollider2D))]
    public sealed class FlashlightPickup2D : MonoBehaviour, IAutoPickup
    {
        private SingleUsePickupController controller;
        public bool Collected => controller?.Collected ?? false;
        public Vector2 PickupPosition => transform.position;

        private void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        public void RestoreState(bool consumed)
        {
            RestoreCollection(consumed);
        }

        [Inject]
        public void Construct(PickupControllerFactory factory)
        {
            if (controller != null)
            {
                throw new InvalidOperationException("Pickup is already composed.");
            }

            controller = (factory ?? throw new ArgumentNullException(nameof(factory))).CreateSingleUse();
        }

        public bool TryCollect(PlayerInteraction actor)
        {
            if (actor == null || !isActiveAndEnabled)
            {
                return false;
            }

            return RequireController().TryCollect(new Operation(this, actor)).Success;
        }

        private SingleUsePickupController RequireController() => controller ?? throw new InvalidOperationException("Pickup requires level composition.");

        private void RestoreCollection(bool consumed)
        {
            var result = RequireController().Restore(consumed);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        private readonly struct Operation : IPickupOperation
        {
            private readonly FlashlightPickup2D owner;
            private readonly PlayerInteraction actor;

            public Operation(FlashlightPickup2D owner, PlayerInteraction actor)
            {
                this.owner = owner;
                this.actor = actor;
            }

            public bool CanApply => owner.isActiveAndEnabled && actor.Flashlight != null && actor.CanReach(owner.PickupPosition, 1);
            public bool Apply() => actor.Flashlight.Acquire();

            public void Present()
            {
                try
                {
                    actor.ReportPickup(PickupKind.Flashlight, "Фонарь");
                }
                finally
                {
                    owner.gameObject.SetActive(false);
                }
            }
        }
    }
}
