using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift.Editor;
using EmergencyShift.Features.MainMenu.Config;
using EmergencyShift.Features.MainMenu.Logic;
using EmergencyShift.Features.MainMenu.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerifyUniversalMenu
{
    public static object Finish()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Finish in Edit Mode.");
        }
        Scene menu = SceneManager.GetSceneByPath(MainMenuAuthoring.ScenePath);
        if (!menu.IsValid() || !menu.isLoaded)
        {
            menu = EditorSceneManager.OpenScene(MainMenuAuthoring.ScenePath, OpenSceneMode.Additive);
        }
        SceneManager.SetActiveScene(menu);
        for (int index = SceneManager.sceneCount - 1; index >= 0; index--)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            if (scene.path.StartsWith("Assets/InitTestScene", StringComparison.Ordinal))
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        MainMenuEntryPolicy.Synchronize();
        return new { active = SceneManager.GetActiveScene().path,
            entry = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            buildScenes = EditorBuildSettings.scenes.Length, authoredScenesSaved = 0 };
    }

    public static object Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Prepare in Edit Mode.");
        }
        MainMenuEntryPolicy.Synchronize();
        // Deliberately reproduce the reported regression; the Play hook must repair it.
        EditorSceneManager.playModeStartScene = null;
        return new { openScene = SceneManager.GetActiveScene().path, entryReset = true };
    }

    public static async Task<object> Exercise(int start, int count)
    {
        if (SceneManager.GetActiveScene().path != MainMenuAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Ordinary Play failed to enforce MainMenu after a reset.");
        }
        InputSettings original = InputSystem.settings;
        var testSettings = UnityEngine.Object.Instantiate(original);
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = testSettings;
        InputSystem.EnableDevice(Keyboard.current);
        try
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MainMenuAuthoring.CatalogPath).ToCatalog();
            var view = UnityEngine.Object.FindAnyObjectByType<MainMenuView>();
            view.Request(MenuPage.Scenes);
            ScrollRect scroll = view.GetComponentInChildren<ScrollRect>();
            MenuSceneRowView[] rows = view.GetComponentsInChildren<MenuSceneRowView>();
            if (rows.Length != catalog.Count)
            {
                throw new InvalidOperationException("Visible catalog differs from scene inventory.");
            }
            EventSystem.current.SetSelectedGameObject(rows[rows.Length - 1].gameObject);
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, rows[rows.Length - 1].transform);
            if (bounds.min.y < scroll.viewport.rect.yMin - 1 || bounds.max.y > scroll.viewport.rect.yMax + 1)
            {
                throw new InvalidOperationException("Last keyboard-selected row is outside the viewport.");
            }
            var paths = Enumerable.Range(start, count).Select(index => catalog.At(index).Path).ToArray();
            foreach (string path in paths)
            {
                await MainMenuEditorNavigation.LaunchScene(path);
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.F8));
                await Task.Delay(100);
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                DateTime deadline = DateTime.UtcNow.AddSeconds(20);
                while (SceneManager.GetActiveScene().path != MainMenuAuthoring.ScenePath)
                {
                    if (DateTime.UtcNow > deadline)
                    {
                        throw new TimeoutException("F8 failed from " + path);
                    }
                    await Task.Delay(50);
                }
                await Task.Delay(300);
                if (UnityEngine.Object.FindObjectsByType<MainMenuPresenter>().Length != 1
                    || UnityEngine.Object.FindObjectsByType<AudioListener>().Length != 1
                    || UnityEngine.Object.FindObjectsByType<EventSystem>().Length != 1)
                {
                    throw new InvalidOperationException("Scene transition leaked or lost the menu context.");
                }
            }
            var result = new { catalogScenes = catalog.Count, lastRowVisible = true, repairedEntry = true,
                paths, roundTrips = paths.Length, start };
            Directory.CreateDirectory("work/universal-menu");
            File.WriteAllText("work/universal-menu/routes-" + start + ".json",
                Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.settings = original;
            UnityEngine.Object.Destroy(testSettings);
        }
    }
}
