using System;
using EmergencyShift.Features.MainMenu.Logic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmergencyShift.Features.MainMenu.View
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject[] pages;
        [SerializeField] private UnityEngine.UI.Selectable[] firstSelection;
        [SerializeField] private UnityEngine.UI.Button newGame;
        [SerializeField] private UnityEngine.UI.Button quit;
        [SerializeField] private UnityEngine.UI.Button apply;
        [SerializeField] private GameObject ownerAccess;
        [SerializeField] private UnityEngine.UI.Slider volume;
        [SerializeField] private UnityEngine.UI.Toggle fullscreen;
        [SerializeField] private UnityEngine.UI.Text status;
        [SerializeField] private UnityEngine.UI.Text volumeValue;
        [SerializeField] private Transform sceneRows;
        [SerializeField] private MenuSceneRowView rowPrefab;
        private string newGameId;
        public event Action<MenuPage> PageRequested;
        public event Action<string> LaunchRequested;
        public event Action QuitRequested;
        public event Action<MenuPreferences> PreferencesRequested;

        private void Awake()
        {
            foreach (MenuPageButton button in GetComponentsInChildren<MenuPageButton>(true))
            {
                button.Bind(this);
            }

            newGame.onClick.AddListener(NewGame);
            quit.onClick.AddListener(Quit);
            apply.onClick.AddListener(Apply);
            volume.onValueChanged.AddListener(UpdateVolume);
        }

        private void OnDestroy()
        {
            newGame.onClick.RemoveListener(NewGame);
            quit.onClick.RemoveListener(Quit);
            apply.onClick.RemoveListener(Apply);
            volume.onValueChanged.RemoveListener(UpdateVolume);
        }

        public void Initialize(MenuSceneCatalog catalog, IMenuNavigation navigation)
        {
            newGameId = catalog.NewGameId;
            ownerAccess.SetActive(navigation.OwnerAccess);
            for (int index = 0; index < catalog.Count; index++)
            {
                MenuScene scene = catalog.At(index);
                MenuSceneRowView row = Instantiate(rowPrefab, sceneRows);
                row.Bind(scene, navigation.CanLoad(scene.Path), Launch);
            }
        }

        public void Present(MainMenuModel model, MenuPreferences preferences)
        {
            for (int index = 0; index < pages.Length; index++)
            {
                if (pages[index] != null)
                {
                    pages[index].SetActive(index == (int)model.Page);
                }
            }

            if (model.Page == MenuPage.Settings)
            {
                volume.SetValueWithoutNotify(preferences.volume);
                fullscreen.SetIsOnWithoutNotify(preferences.fullscreen);
                UpdateVolume(preferences.volume);
            }

            status.text = model.Error ?? string.Empty;
            UnityEngine.UI.Selectable selection = firstSelection[(int)model.Page];
            if (selection != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selection.gameObject);
            }
        }

        public void Request(MenuPage page) => PageRequested?.Invoke(page);
        public void SetInteractable(bool value) => GetComponent<CanvasGroup>().interactable = value;
        public void ShowStatus(string message) => status.text = message ?? string.Empty;
        private void NewGame() => Launch(newGameId);
        private void Launch(string id) => LaunchRequested?.Invoke(id);
        private void Quit() => QuitRequested?.Invoke();
        private void Apply() => PreferencesRequested?.Invoke(new MenuPreferences(volume.value, fullscreen.isOn));
        private void UpdateVolume(float value) => volumeValue.text = Mathf.RoundToInt(value * 100) + "%";
    }
}
