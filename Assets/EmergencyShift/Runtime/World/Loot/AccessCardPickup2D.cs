using UnityEngine;

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
        public bool Collected
        {
            get; private set;
        }

        public void RestoreState(bool consumed)
        {
            Collected = consumed;
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

        public bool TryCollect(PlayerInteraction actor)
        {
            if (Collected || !isActiveAndEnabled || actor == null || !actor.CanReach(PickupPosition, 1))
            {
                return false;
            }

            var inventory = actor.GetComponent<PlayerAccessCards2D>();
            if (inventory == null || !inventory.Add(cardId))
            {
                return false;
            }

            Collected = true;
            actor.ReportPickup(PickupKind.AccessCard, displayName);
            gameObject.SetActive(false);
            return true;
        }
    }
}
