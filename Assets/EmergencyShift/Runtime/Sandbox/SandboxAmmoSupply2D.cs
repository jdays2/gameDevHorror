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

        public bool IsAvailable(IInteractionActor actor)
        {
            return isActiveAndEnabled && actor is IWeaponRecipient recipient && recipient.CanCollectWeapon
                && actor.CanReach(InteractionPosition, interactionRange);
        }

        public string GetPrompt(IInteractionActor actor)
        {
            var recipient = actor as IWeaponRecipient;
            if (recipient == null || !recipient.HasWeapon)
            {
                return "ЯЩИК ПАТРОНОВ · Сначала подберите оружие";
            }

            if (recipient.Ammo >= recipient.MaximumAmmo)
            {
                return "ЯЩИК ПАТРОНОВ · Запас полный";
            }

            return "[E] Пополнить «" + recipient.WeaponName + "» · без ограничений";
        }

        public bool Interact(IInteractionActor actor)
        {
            if (!IsAvailable(actor) || !(actor is IWeaponRecipient recipient) || !recipient.HasWeapon)
            {
                return false;
            }

            int received = recipient.CollectAmmo(recipient.MaximumAmmo);
            if (received == 0)
            {
                return false;
            }

            actor.ReportPickup(PickupKind.Ammo, "Патроны +" + received);
            return true;
        }
    }
}
