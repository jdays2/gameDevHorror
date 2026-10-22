using System;
using EmergencyShift.Features.Pickups;
using EmergencyShift.Features.Pickups.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(CircleCollider2D))]
    public sealed class AccessCardPickup2D : MonoBehaviour, IAutoPickup
    {
        [SerializeField]
        private string cardId = "service";
        [SerializeField]
        private string displayName = "Служебная карта";
        public string CardId => cardId;
        private SingleUsePickupController controller;
        public bool Collected => controller?.Collected ?? false;

        public void RestoreState(bool consumed)
        {
            RestoreCollection(consumed);
        }

        public Vector2 PickupPosition => transform.position;

        public void Configure(string id, string label)
        {
            cardId = id;
            displayName = label;
        }

        private void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
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

        public bool TryCollect(IInteractionActor actor)
        {
            if (!(actor is IAccessCardRecipient recipient) || !isActiveAndEnabled)
            {
                return false;
            }

            return RequireController().TryCollect(new Operation(this, recipient)).Success;
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
            private readonly AccessCardPickup2D owner;
            private readonly IAccessCardRecipient actor;

            public Operation(AccessCardPickup2D owner, IAccessCardRecipient actor)
            {
                this.owner = owner;
                this.actor = actor;
            }

            public bool CanApply => owner.isActiveAndEnabled && actor.CanReach(owner.PickupPosition, 1);
            public bool Apply()
            {
                return actor.TryCollectCard(owner.cardId);
            }

            public void Present()
            {
                try
                {
                    actor.ReportPickup(PickupKind.AccessCard, owner.displayName);
                }
                finally
                {
                    owner.gameObject.SetActive(false);
                }
            }
        }
    }
}
