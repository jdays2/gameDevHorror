using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Optional card lock and reader events; Door2D owns interaction and movement.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Door2D))]
    public sealed class DoorAccess2D : MonoBehaviour
    {
        [SerializeField]
        private string requiredCardId = "service";
        [SerializeField]
        private string cardName = "Служебная карта";
        [SerializeField]
        private DoorAccessFeedbackProfile feedbackProfile;
        private float nextNotice;
        private float nextDeniedFeedback;
        public event System.Action<bool> AccessChecked;
        public DoorAccessFeedbackProfile FeedbackProfile => feedbackProfile;
        public float ConfirmationDelay => feedbackProfile != null ? feedbackProfile.confirmationDelay : 0;
        public string RequiredCardId => requiredCardId;
        public bool IsUnlocked
        {
            get; private set;
        }

        public void RestoreState(bool unlocked)
        {
            IsUnlocked = unlocked;
            nextDeniedFeedback = 0;
            nextNotice = 0;
        }

        public void ConfigureFeedback(DoorAccessFeedbackProfile profile)
        {
            feedbackProfile = profile;
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
                AccessChecked?.Invoke(true);
                return true;
            }

            if (Time.time >= nextDeniedFeedback)
            {
                nextDeniedFeedback = Time.time + (feedbackProfile != null ? feedbackProfile.denialCooldown : .8f);
                AccessChecked?.Invoke(false);
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
