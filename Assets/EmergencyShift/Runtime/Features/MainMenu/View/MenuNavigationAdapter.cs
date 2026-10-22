using System;
using System.Collections;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.MainMenu.Logic;
using EmergencyShift.Features.PauseMenu.Config;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zenject;

namespace EmergencyShift.Features.MainMenu.View
{
    /// <summary>ProjectContext owns this adapter; Single loads dispose the departing scene's scope.</summary>
    public sealed class MenuNavigationAdapter : MonoBehaviour, IMenuNavigation
    {
        [SerializeField] private PauseMenuAudioConfig transitionAudio;
        private MenuSceneCatalog catalog;
        private InputAction returnAction;
        private InputAction confirmation;
        private bool busy;
        public bool OwnerAccess => Application.isEditor || Debug.isDebugBuild;
        public bool IsBusy => busy;
        public event Action<string> LoadFailed;
        public event Action OwnerMenuRequested;

        [Inject]
        private void Construct(MenuSceneCatalog catalog)
        {
            this.catalog = catalog;
        }

        private void Start()
        {
            if (catalog == null)
            {
                throw new InvalidOperationException("Menu navigation requires world composition.");
            }
            if (transitionAudio == null)
            {
                throw new InvalidOperationException("Menu navigation requires its authored transition audio config.");
            }
            transitionAudio.Validate();

            confirmation = new InputAction("Menu confirmation held", InputActionType.Button);
            confirmation.AddBinding("<Mouse>/leftButton");
            confirmation.AddBinding("<Keyboard>/enter");
            confirmation.AddBinding("<Keyboard>/space");
            confirmation.AddBinding("<Gamepad>/buttonSouth");
            confirmation.Enable();

            if (OwnerAccess)
            {
                returnAction = new InputAction("Owner menu", InputActionType.Button, "<Keyboard>/f8");
                returnAction.Enable();
            }
        }

        private void Update()
        {
            if (returnAction == null || !returnAction.WasPressedThisFrame() || busy)
            {
                return;
            }

            if (SceneManager.GetActiveScene().path == catalog.HomePath)
            {
                OwnerMenuRequested?.Invoke();
            }
            else
            {
                Load(catalog.HomePath);
            }
        }

        private void OnDestroy()
        {
            returnAction?.Dispose();
            returnAction = null;
            confirmation?.Dispose();
            confirmation = null;
        }

        public bool CanLoad(string scenePath) => !string.IsNullOrWhiteSpace(scenePath) && Application.CanStreamedLevelBeLoaded(scenePath);

        public CommandResult Load(string scenePath)
        {
            if (busy || !CanLoad(scenePath))
            {
                return CommandResult.Fail("Сцена отсутствует в сборке или переход уже выполняется.");
            }

            busy = true;
            StartCoroutine(LoadAfterRelease(scenePath));
            return CommandResult.Ok();
        }

        public void Quit() => Application.Quit();

        private IEnumerator LoadAfterRelease(string scenePath)
        {
            // A held submit/click belongs to the menu, not to the newly enabled gameplay actions.
            while (confirmation != null && confirmation.IsPressed())
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(transitionAudio.FadeOutSeconds);

            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            }
            catch (Exception error)
            {
                busy = false;
                LoadFailed?.Invoke(error.Message);
                yield break;
            }

            if (operation == null)
            {
                busy = false;
                LoadFailed?.Invoke("Не удалось начать загрузку сцены.");
                yield break;
            }

            yield return operation;
            busy = false;
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                LoadFailed?.Invoke("Загружена другая сцена; повторите выбор.");
            }
        }
    }
}
