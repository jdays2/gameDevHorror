using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Пистолет или коробка патронов на земле; использует общий выбор одной цели по E.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CircleCollider2D))]
    public sealed class WeaponLootPickup : MonoBehaviour, IInteractable
    {
        public enum LootKind { Pistol, Ammo }
        [SerializeField] private LootKind kind;
        [SerializeField, Min(1)] private int cartridges = 8;
        [SerializeField, Min(0.1f)] private float pickupRange = 1f;
        [SerializeField] private bool requiresLight = true;
        private bool collected;
        public LootKind Kind => kind;
        public bool Collected => collected;
        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => pickupRange;
        public void Configure(LootKind type, int amount) { kind = type; cartridges = amount; }
        private void Reset() => GetComponent<CircleCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;

        public bool IsAvailable(PlayerInteraction actor)
        {
            return !collected && isActiveAndEnabled && actor != null && actor.Weapon != null
                && actor.CanReach(InteractionPosition, pickupRange)
                && (!requiresLight || actor.IsCurrentlyLit(InteractionPosition));
        }
        public string GetPrompt(PlayerInteraction actor)
        {
            if (kind == LootKind.Pistol && actor.Weapon.HasWeapon) return "Пистолет уже взят";
            if (kind == LootKind.Ammo && actor.Weapon.Ammo >= actor.Weapon.MaximumAmmo) return "Запас патронов полон";
            return kind == LootKind.Pistol ? "[E]  Подобрать пистолет" : "[E]  Подобрать патроны";
        }
        public bool Interact(PlayerInteraction actor)
        {
            if (!IsAvailable(actor)) return false;
            bool success = kind == LootKind.Pistol ? actor.Weapon.Acquire(cartridges) : actor.Weapon.TryAddAmmo(cartridges);
            if (!success) return false;
            collected = true; gameObject.SetActive(false); return true;
        }
    }
}
