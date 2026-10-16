using UnityEngine;

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
        private bool collected;
        public LootKind Kind => kind;
        public bool Collected => collected;
        public int RemainingCartridges => cartridges;

        public void RestoreState(bool consumed, int remaining)
        {
            collected = consumed;
            cartridges = Mathf.Max(0, remaining);
        }

        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => pickupRange;
        public Vector2 PickupPosition => transform.position;

        public void Configure(LootKind type, int amount, WeaponDefinition selected = null)
        {
            kind = type;
            cartridges = amount;
            if (selected != null)
            {
                weaponDefinition = selected;
            }
        }

        private void Reset() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;
        public bool IsAvailable(PlayerInteraction actor)
        {
            return !collected && isActiveAndEnabled && actor != null && actor.Weapon != null && actor.CanReach(InteractionPosition, pickupRange);
        }

        public bool TryCollect(PlayerInteraction actor)
        {
            if (!IsAvailable(actor))
            {
                return false;
            }

            int received = 0;
            bool success;
            if (kind == LootKind.Weapon)
            {
                success = actor.Weapon.Acquire(cartridges, weaponDefinition);
            }
            else
            {
                received = actor.Weapon.AddAmmo(cartridges, weaponDefinition);
                success = received > 0;
            }
            if (!success)
            {
                return false;
            }

            if (kind == LootKind.Ammo)
            {
                cartridges -= received;
                actor.ReportPickup(PickupKind.Ammo, "Патроны +" + received);
                if (cartridges > 0)
                {
                    return true; // Preserve the remainder of a partially collected box.
                }
            }
            else
            {
                actor.ReportPickup(PickupKind.Weapon, actor.Weapon.Definition != null ? actor.Weapon.Definition.displayName : "Пистолет");
            }

            collected = true;
            gameObject.SetActive(false);
            return true;
        }
    }
}
