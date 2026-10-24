using System;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift.Features.MainMenu.Config;
using EmergencyShift.Features.MainMenu.Logic;
using EmergencyShift.Features.MainMenu.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Editor probes launch through the same menu buttons as the owner.</summary>
    public static class MainMenuEditorNavigation
    {
        public static async Task LaunchScene(string scenePath)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Start Play from MainMenu before selecting a scene.");
            }
            if (SceneManager.GetActiveScene().path == scenePath)
            {
                return;
            }
            var view = UnityEngine.Object.FindAnyObjectByType<MainMenuView>();
            if (view == null)
            {
                throw new InvalidOperationException("Return to MainMenu with F8 before selecting another scene.");
            }
            var catalog = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MainMenuAuthoring.CatalogPath).ToCatalog();
            MenuScene scene = Enumerable.Range(0, catalog.Count).Select(catalog.At).SingleOrDefault(entry => entry.Path == scenePath);
            if (scene == null)
            {
                throw new ArgumentException("Scene is absent from the menu catalog: " + scenePath);
            }
            view.Request(MenuPage.Scenes);
            var rows = view.GetComponentsInChildren<MenuSceneRowView>();
            Button button = rows.Single(row => row.SceneId == scene.Id).GetComponent<Button>();
            if (!button.interactable)
            {
                throw new InvalidOperationException("Scene is unavailable: " + scenePath);
            }
            button.onClick.Invoke();
            DateTime deadline = DateTime.UtcNow.AddSeconds(20);
            while (SceneManager.GetActiveScene().path != scenePath)
            {
                if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("Menu transition did not finish: " + scenePath);
                }
                await Task.Delay(50);
            }
            await Task.Delay(200);
        }
    }
}
