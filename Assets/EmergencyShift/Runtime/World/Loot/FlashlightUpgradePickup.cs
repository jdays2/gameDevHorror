using System;
using EmergencyShift.Features.Pickups;
using EmergencyShift.Features.Pickups.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    /// <summary>Proximity pickup; preserves repeatable IDs and upgrade caps.</summary>
    public sealed class FlashlightUpgradePickup : MonoBehaviour, IAutoPickup
    {
        [SerializeField, Tooltip("ID вида модуля. При Repeatable каждая новая копия даёт ещё один бонус.")]
        private string moduleId = "range-lens-01";
        [SerializeField]
        private string displayName = "Модуль фонаря";
        [SerializeField, Min(0)]
        private float rangeBonus = 0.6f;
        [SerializeField, Min(0)]
        private float angleBonus = 6f;
        [SerializeField, Min(0.1f)]
        private float pickupRange = 1f;
        [SerializeField, Tooltip("Можно собирать несколько экземпляров этого вида; каждый предмет всё равно однократный.")]
        private bool repeatable = true;
        private SingleUsePickupController controller;
        public string ModuleId => moduleId;
        public bool Collected => controller?.Collected ?? false;

        public void RestoreState(bool consumed)
        {
            RestoreCollection(consumed);
        }

        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => pickupRange;
        public Vector2 PickupPosition => transform.position;

        private void Reset() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void OnValidate()
        {
            rangeBonus = Mathf.Max(0, rangeBonus);
            angleBonus = Mathf.Max(0, angleBonus);
            pickupRange = Mathf.Max(0.1f, pickupRange);
            if (moduleId != null)
            {
                moduleId = moduleId.Trim();
            }
        }

        public bool IsAvailable(PlayerInteraction actor)
        {
            if (Collected || !isActiveAndEnabled || actor == null || actor.Upgrades == null)
            {
                return false;
            }

            if (!actor.CanReach(InteractionPosition, pickupRange))
            {
                return false;
            }

            return true;
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
            private readonly FlashlightUpgradePickup owner;
            private readonly PlayerInteraction actor;

            public Operation(FlashlightUpgradePickup owner, PlayerInteraction actor)
            {
                this.owner = owner;
                this.actor = actor;
            }

            public bool CanApply => owner.IsAvailable(actor);
            public bool Apply() => actor.Upgrades.TryInstall(owner.moduleId, owner.displayName,
                owner.rangeBonus, owner.angleBonus, out _, owner.repeatable);

            public void Present()
            {
                try
                {
                    actor.ReportPickup(PickupKind.FlashlightUpgrade, owner.displayName);
                }
                finally
                {
                    owner.gameObject.SetActive(false);
                }
            }
        }
    }
}
