using System;
using EmergencyShift.Features.AccessControl;
using EmergencyShift.Features.AccessControl.Config;
using EmergencyShift.Features.AccessControl.Logic;
using UnityEngine;
using Zenject;

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
        private DoorAccessControllerFactory factory;
        private DoorAccessController controller;

        public DoorAccessFeedbackProfile FeedbackProfile => feedbackProfile;
        public float ConfirmationDelay => feedbackProfile != null ? feedbackProfile.ConfirmationDelay : 0;
        public string RequiredCardId => requiredCardId;
        public bool IsUnlocked => controller != null && controller.IsUnlocked;
        public event Action<bool> AccessChecked;

        [Inject]
        public void Construct(DoorAccessControllerFactory controllerFactory)
        {
            factory = controllerFactory ?? throw new ArgumentNullException(nameof(controllerFactory));
        }

        public void RestoreState(bool unlocked)
        {
            RequireController().Restore(unlocked);
        }

        public void ConfigureFeedback(DoorAccessFeedbackProfile profile)
        {
            if (profile != null)
            {
                profile.Validate();
            }

            feedbackProfile = profile;
            controller?.Configure(Settings());
        }

        public void Configure(string id, string label)
        {
            requiredCardId = id;
            cardName = label;
            controller?.Configure(Settings());
        }

        private bool HasCard(PlayerInteraction actor)
        {
            var cards = actor != null ? actor.GetComponent<PlayerAccessCards2D>() : null;
            return cards != null && cards.Has(requiredCardId);
        }

        public string Prompt(PlayerInteraction actor) => IsUnlocked || HasCard(actor) ? "[E]  Открыть дверь" : "ЗАПЕРТО · Нужна «" + cardName + "»  [E]";
        public bool TryUnlock(PlayerInteraction actor)
        {
            DoorAccessAttempt result = RequireController().TryUnlock(HasCard(actor), actor != null);
            if (result.HasFeedback)
            {
                AccessChecked?.Invoke(result.Granted);
            }

            if (result.ShowNotice)
            {
                actor.ShowNotification("Нет доступа: нужна «" + cardName + "»");
            }

            return result.Granted;
        }

        private void Awake()
        {
            if (feedbackProfile != null)
            {
                feedbackProfile.Validate();
            }
        }

        private DoorAccessSettings Settings() => new DoorAccessSettings(requiredCardId,
            feedbackProfile != null ? feedbackProfile.DenialCooldown : .8f, 1.8f);

        private DoorAccessController RequireController()
        {
            if (factory == null)
            {
                throw new InvalidOperationException("Door access requires level composition.");
            }

            return controller ??= factory.Create(Settings());
        }
    }
}
