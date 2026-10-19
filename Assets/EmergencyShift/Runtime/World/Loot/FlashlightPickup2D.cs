using UnityEngine;

namespace EmergencyShift
{
    /// <summary>One scene-authored flashlight; proximity and wall checks use the common pickup contract.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CircleCollider2D))]
    public sealed class FlashlightPickup2D : MonoBehaviour, IAutoPickup
    {
        public bool Collected { get; private set; }
        public Vector2 PickupPosition => transform.position;

        private void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        public void RestoreState(bool consumed)
        {
            Collected = consumed;
        }

        public bool TryCollect(PlayerInteraction actor)
        {
            if (Collected || !isActiveAndEnabled || actor == null || actor.Flashlight == null ||
                !actor.CanReach(PickupPosition, 1) || !actor.Flashlight.Acquire())
            {
                return false;
            }

            Collected = true;
            actor.ReportPickup(PickupKind.Flashlight, "Фонарь");
            gameObject.SetActive(false);
            return true;
        }
    }
}
