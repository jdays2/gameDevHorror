using System;
using EmergencyShift.Features.Pickups;
using EmergencyShift.Features.Pickups.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Automatic ground loot; full capacity leaves ammo on the floor.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CircleCollider2D))]
    public sealed class WeaponLootPickup : MonoBehaviour, IAutoPickup
    {
        public enum LootKind
        {
            Weapon,
            Ammo
        }

        [SerializeField]
        private LootKind kind;
        [SerializeField]
        private WeaponDefinition weaponDefinition;
        [SerializeField, Min(1)]
        private int cartridges = 8;
        [SerializeField, Min(0.1f)]
        private float pickupRange = 1f;
        private WeaponLootController controller;
        public LootKind Kind => kind;
        public bool Collected => controller?.Collected ?? false;
        public int RemainingCartridges => controller?.Remaining ?? cartridges;

        public void RestoreState(bool consumed, int remaining)
        {
            var result = RequireController().Restore(consumed, Mathf.Max(0, remaining));
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => pickupRange;
        public Vector2 PickupPosition => transform.position;

        public void Configure(LootKind type, int amount, WeaponDefinition selected = null)
        {
            ValidateKind(type);
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (controller != null)
            {
                var result = controller.Configure(type == LootKind.Ammo, amount);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Reason);
                }
            }

            kind = type;
            cartridges = amount;
            if (selected != null)
            {
                weaponDefinition = selected;
            }
        }

        private void Reset() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;
        public bool IsAvailable(IInteractionActor actor)
        {
            return !Collected && isActiveAndEnabled && actor is IWeaponRecipient recipient && recipient.CanCollectWeapon && actor.CanReach(InteractionPosition, pickupRange);
        }

        [Inject]
        public void Construct(PickupControllerFactory factory)
        {
            if (controller != null)
            {
                throw new InvalidOperationException("Weapon loot is already composed.");
            }

            ValidateKind(kind);
            controller = (factory ?? throw new ArgumentNullException(nameof(factory))).CreateWeaponLoot(kind == LootKind.Ammo, cartridges);
        }

        public bool TryCollect(IInteractionActor actor)
        {
            if (!(actor is IWeaponRecipient recipient) || !isActiveAndEnabled)
            {
                return false;
            }

            return RequireController().TryCollect(new Operation(this, recipient)).Success;
        }

        private WeaponLootController RequireController() => controller ?? throw new InvalidOperationException("Weapon loot requires level composition.");

        private static void ValidateKind(LootKind value)
        {
            if (value != LootKind.Weapon && value != LootKind.Ammo)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private readonly struct Operation : IWeaponLootOperation
        {
            private readonly WeaponLootPickup owner;
            private readonly IWeaponRecipient actor;

            public Operation(WeaponLootPickup owner, IWeaponRecipient actor)
            {
                this.owner = owner;
                this.actor = actor;
            }

            public bool CanApply => owner.IsAvailable(actor);
            public WeaponLootGrant Apply(bool isAmmo, int amount) => isAmmo
                ? WeaponLootGrant.Ammo(actor.CollectAmmo(amount, owner.weaponDefinition))
                : WeaponLootGrant.Weapon(actor.CollectWeapon(amount, owner.weaponDefinition));

            public void Present(bool isAmmo, int received, bool consumed)
            {
                try
                {
                    if (isAmmo)
                    {
                        actor.ReportPickup(PickupKind.Ammo, "Патроны +" + received);
                    }
                    else
                    {
                        actor.ReportPickup(PickupKind.Weapon, actor.WeaponName);
                    }
                }
                finally
                {
                    if (consumed)
                    {
                        owner.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
