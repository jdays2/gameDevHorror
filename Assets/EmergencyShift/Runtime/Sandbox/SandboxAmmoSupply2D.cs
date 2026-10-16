using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Reusable sandbox supply; refills reserve through the normal inventory limits.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class SandboxAmmoSupply2D : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(.1f)]
        private float interactionRange = 1.6f;

        public Vector2 InteractionPosition => transform.position;
        public float InteractionRange => interactionRange;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        public bool IsAvailable(PlayerInteraction actor)
        {
            return isActiveAndEnabled && actor != null && actor.Weapon != null
                && actor.CanReach(InteractionPosition, interactionRange);
        }

        public string GetPrompt(PlayerInteraction actor)
        {
            if (!actor.Weapon.HasWeapon)
            {
                return "ЯЩИК ПАТРОНОВ · Сначала подберите оружие";
            }

            if (actor.Weapon.Ammo >= actor.Weapon.MaximumAmmo)
            {
                return "ЯЩИК ПАТРОНОВ · Запас полный";
            }

            return "[E] Пополнить «" + actor.Weapon.Definition.displayName + "» · без ограничений";
        }

        public bool Interact(PlayerInteraction actor)
        {
            if (!IsAvailable(actor) || !actor.Weapon.HasWeapon)
            {
                return false;
            }

            int received = actor.Weapon.AddAmmo(actor.Weapon.MaximumAmmo);
            if (received == 0)
            {
                return false;
            }

            actor.ReportPickup(PickupKind.Ammo, "Патроны +" + received);
            return true;
        }
    }
}
