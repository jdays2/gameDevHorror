using System;
using EmergencyShift.Features.DeathScreen.Logic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmergencyShift.Features.DeathScreen.View
{
    [DefaultExecutionOrder(-450)]
    public sealed class DeathScreenView : MonoBehaviour
    {
        [SerializeField] private Canvas overlay;
        [SerializeField] private CanvasGroup shade;
        [SerializeField] private DeathScreenIris iris;
        [SerializeField] private CanvasGroup blackout;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private CanvasGroup loading;
        [SerializeField] private UnityEngine.UI.Text title;
        [SerializeField] private UnityEngine.UI.Text message;
        [SerializeField] private UnityEngine.UI.Text primaryLabel;
        [SerializeField] private UnityEngine.UI.Text secondaryLabel;
        [SerializeField] private UnityEngine.UI.Text hint;
        [SerializeField] private DeathScreenButton primary;
        [SerializeField] private DeathScreenButton secondary;
        [SerializeField] private DeathScreenGameplayOverlay gameplay;
        [SerializeField] private GameObject fallbackEvents;
        [SerializeField] private Color errorColor = new Color(.84f, .74f, .72f);
        private Color messageColor;
        private bool visible;
        private bool ownsEvents;
        private DeathScreenPhase displayedPhase = (DeathScreenPhase)(-1);
        private GameObject previousSelection;

        public bool IsVisible => visible;
        public event Action PrimaryRequested;
        public event Action SecondaryRequested;

        private void Awake()
        {
            messageColor = message.color;
            overlay.enabled = false;
            primary.onClick.AddListener(OnPrimary);
            secondary.onClick.AddListener(OnSecondary);
        }

        private void OnDestroy()
        {
            if (primary != null)
            {
                primary.onClick.RemoveListener(OnPrimary);
            }
            if (secondary != null)
            {
                secondary.onClick.RemoveListener(OnSecondary);
            }
            Hide();
        }

        public void Render(DeathScreenModel model)
        {
            if (!model.IsVisible)
            {
                Hide();
                return;
            }
            Show();
            shade.alpha = model.WorldOpacity;
            iris.Render(model.ClosureProgress, model.Settings.IrisFeather);
            blackout.alpha = model.BlackoutOpacity;
            content.alpha = model.ContentOpacity;
            loading.alpha = model.Phase == DeathScreenPhase.Loading
                ? Mathf.Clamp01(model.Elapsed / Mathf.Max(.001f, model.Settings.ContentFadeSeconds)) : 0;
            content.interactable = model.IsInteractive;
            primary.interactable = model.IsInteractive;
            secondary.interactable = model.IsInteractive;
            if (displayedPhase != model.Phase)
            {
                displayedPhase = model.Phase;
                RefreshText(model);
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    if (model.IsInteractive)
                    {
                        EventSystem.current.SetSelectedGameObject(primary.gameObject);
                    }
                }
            }
        }

        public void Hide()
        {
            if (!visible)
            {
                return;
            }
            visible = false;
            displayedPhase = (DeathScreenPhase)(-1);
            if (overlay != null)
            {
                overlay.enabled = false;
            }
            if (gameplay != null)
            {
                gameplay.Restore();
            }
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(previousSelection != null &&
                    previousSelection.activeInHierarchy ? previousSelection : null);
            }
            previousSelection = null;
            if (ownsEvents && fallbackEvents != null)
            {
                fallbackEvents.SetActive(false);
            }
            ownsEvents = false;
        }

        private void Show()
        {
            if (visible)
            {
                return;
            }
            visible = true;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            gameplay.Capture();
            if (EventSystem.current == null && fallbackEvents != null)
            {
                fallbackEvents.SetActive(true);
                ownsEvents = true;
            }
            overlay.enabled = true;
        }

        private void RefreshText(DeathScreenModel model)
        {
            bool confirm = model.Phase == DeathScreenPhase.ConfirmQuit;
            bool error = model.Phase == DeathScreenPhase.Error;
            bool waiting = model.Phase == DeathScreenPhase.WaitingRetry ||
                model.Phase == DeathScreenPhase.WaitingQuit;
            bool transition = waiting || model.Phase == DeathScreenPhase.LeavingRetry ||
                model.Phase == DeathScreenPhase.LeavingQuit || model.Phase == DeathScreenPhase.Loading;
            title.text = model.IsRecoveryError ? "ОШИБКА" : "ВЫ ПОГИБЛИ";
            message.color = error ? errorColor : messageColor;
            if (confirm)
            {
                message.text = "Выйти из игры?";
                primaryLabel.text = "Остаться";
                secondaryLabel.text = "Выйти";
                hint.text = "↑ ↓ Выбор   Enter Подтвердить   Esc Остаться";
            }
            else if (error)
            {
                message.text = model.Error;
                primaryLabel.text = "Попробовать снова";
                secondaryLabel.text = "Выйти из игры";
                hint.text = "↑ ↓ Выбор   Enter Подтвердить   R Повторить";
            }
            else
            {
                message.text = model.HasCheckpoint ? "Сохранённое состояние" : "С начала сцены";
                primaryLabel.text = model.HasCheckpoint ? "С контрольной точки" : "Повторить попытку";
                secondaryLabel.text = "Выйти из игры";
                hint.text = "↑ ↓ Выбор   Enter Подтвердить   R Повторить";
            }
            if (transition)
            {
                message.text = "Подождите";
                primaryLabel.text = "Загрузка…";
                hint.text = waiting ? "Отпустите кнопку для продолжения" : "";
            }
        }

        private void OnPrimary() => PrimaryRequested?.Invoke();
        private void OnSecondary() => SecondaryRequested?.Invoke();
    }
}
