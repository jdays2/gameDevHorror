using System.Collections;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.MainMenu.Logic;
using EmergencyShift.Features.PauseMenu.View;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EmergencyShift.Features.MainMenu.View
{
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField] private MainMenuView view;
        [SerializeField] private PauseMenuMusic music;
        private MainMenuController controller;
        private MenuPreferencesController preferences;
        private IMenuNavigation navigation;
        private InputAction cancel;
        private bool quitting;

        [Inject]
        private void Construct(MainMenuController controller, MenuPreferencesController preferences, IMenuNavigation navigation)
        {
            this.controller = controller;
            this.preferences = preferences;
            this.navigation = navigation;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cancel = new InputAction("Menu back", InputActionType.Button, "<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");
            cancel.Enable();
            controller.Model.Changed += Present;
            navigation.OwnerMenuRequested += OpenOwnerMenu;
            view.PageRequested += Open;
            view.LaunchRequested += Launch;
            view.QuitRequested += Quit;
            view.PreferencesRequested += Apply;
            view.Initialize(controller.Model.Catalog, navigation);
            music.SetMenuOpen(true);
            Present();
        }

        private void Update()
        {
            if (!quitting && cancel != null && cancel.WasPressedThisFrame())
            {
                Open(MenuPage.Home);
            }
        }

        private void OnDestroy()
        {
            cancel?.Dispose();
            cancel = null;
            if (controller != null)
            {
                controller.Model.Changed -= Present;
            }

            if (navigation != null)
            {
                navigation.OwnerMenuRequested -= OpenOwnerMenu;
            }

            if (view != null)
            {
                view.PageRequested -= Open;
                view.LaunchRequested -= Launch;
                view.QuitRequested -= Quit;
                view.PreferencesRequested -= Apply;
            }
        }

        private void Present()
        {
            view.Present(controller.Model, preferences.Applied);
            music.SetMenuOpen(controller.Model.Page != MenuPage.Loading);
        }
        private void Open(MenuPage page) => Report(controller.Open(page));
        private void Launch(string id) => Report(controller.Launch(id));
        private void OpenOwnerMenu() => Open(MenuPage.Scenes);

        private void Quit()
        {
            if (!quitting && controller.Model.Page == MenuPage.Quit)
            {
                StartCoroutine(QuitAfterFade());
            }
        }

        private IEnumerator QuitAfterFade()
        {
            quitting = true;
            view.SetInteractable(false);
            music.SetMenuOpen(false);
            yield return new WaitForSecondsRealtime(music.Config.FadeOutSeconds);
            Report(controller.Quit());
            if (Application.isEditor)
            {
                quitting = false;
                view.SetInteractable(true);
                Open(MenuPage.Home);
            }
        }

        private void Apply(MenuPreferences values)
        {
            CommandResult result = preferences.Apply(values);
            view.ShowStatus(result.Success ? "Настройки сохранены." : result.Reason);
        }

        private void Report(CommandResult result)
        {
            if (!result.Success)
            {
                view.ShowStatus(result.Reason);
            }
        }
    }
}
