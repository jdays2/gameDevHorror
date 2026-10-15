using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Optional lock. The door still owns interaction, movement, collision and audio.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Door2D))]
    public sealed class DoorAccess2D : MonoBehaviour
    {
        [SerializeField]
        private string requiredCardId = "service";
        [SerializeField]
        private string cardName = "Служебная карта";
        private float nextNotice;
        public string RequiredCardId => requiredCardId;
        public bool IsUnlocked
        {
            get; private set;
        }

        public void RestoreState(bool unlocked)
        {
            IsUnlocked = unlocked;
        }

        public void Configure(string id, string label)
        {
            requiredCardId = id;
            cardName = label;
        }

        private bool HasCard(PlayerInteraction actor)
        {
            var cards = actor != null ? actor.GetComponent<PlayerAccessCards2D>() : null;
            return cards != null && cards.Has(requiredCardId);
        }

        public string Prompt(PlayerInteraction actor) => IsUnlocked || HasCard(actor) ? "[E]  Открыть дверь" : "ЗАПЕРТО · Нужна «" + cardName + "»  [E]";
        public bool TryUnlock(PlayerInteraction actor)
        {
            if (IsUnlocked)
            {
                return true;
            }

            if (HasCard(actor))
            {
                IsUnlocked = true;
                return true;
            }

            if (actor != null && Time.time >= nextNotice)
            {
                actor.ShowNotification("Нет доступа: нужна «" + cardName + "»");
                nextNotice = Time.time + 1.8f;
            }

            return false;
        }
    }
}
