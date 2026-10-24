using System;
using System.IO;
using System.Linq;
using EmergencyShift.Editor;
using EmergencyShift.Features.MainMenu.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class VerifyMenuIsolation49
{
    public static object Inspect(string label)
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(index => SceneManager.GetSceneAt(index))
            .Select(scene => new { scene.path, scene.isLoaded, scene.isDirty, scene.rootCount }).ToArray();
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
            .Select(canvas => new { canvas.name, scene = canvas.gameObject.scene.path, active = canvas.gameObject.activeInHierarchy,
                canvas.enabled, mode = canvas.renderMode.ToString(), position = canvas.transform.position.ToString(),
                images = canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                    .Where(image => image.name.Contains("background", StringComparison.OrdinalIgnoreCase))
                    .Select(image => new { image.name, color = image.color.ToString(), rect = image.rectTransform.rect.ToString() }).ToArray() }).ToArray();
        var result = new { label, playing = EditorApplication.isPlaying, active = SceneManager.GetActiveScene().path,
            entry = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), scenes, canvases,
            menus = UnityEngine.Object.FindObjectsByType<MainMenuView>(FindObjectsInactive.Include).Length,
            cameras = UnityEngine.Object.FindObjectsByType<Camera>().Select(camera => new { camera.name, scene = camera.gameObject.scene.path }).ToArray(),
            listeners = UnityEngine.Object.FindObjectsByType<AudioListener>().Length,
            eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>().Length };
        Directory.CreateDirectory("Docs/Validation/MenuIsolation49");
        File.WriteAllText("Docs/Validation/MenuIsolation49/" + label + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static object RollbackSetup()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play before workspace cleanup.");
        Scene menu = SceneManager.GetSceneByPath(MainMenuAuthoring.ScenePath);
        Require(!menu.IsValid() || !menu.isDirty, "Preserve unsaved MainMenu edits.");
        Scene authoring = SceneManager.GetSceneByPath("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity");
        if (!authoring.IsValid() || !authoring.isLoaded)
        {
            authoring = EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity", OpenSceneMode.Additive);
        }
        SceneManager.SetActiveScene(authoring);
        if (menu.IsValid() && menu.isLoaded)
        {
            EditorSceneManager.CloseScene(menu, true);
        }
        EditorSceneManager.playModeStartScene = null;
        SessionState.EraseBool("EmergencyShift.MainMenu.AutomatedTestRun");
        return Inspect("rollback-edit");
    }

    public static object VerifyCurrentPlay(string label)
    {
        Require(EditorApplication.isPlaying, "Expected ordinary Play.");
        Require(SceneManager.GetActiveScene().path == "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity", "Play did not start from the open level.");
        Require(EditorSceneManager.playModeStartScene == null, "Play entry override survived rollback.");
        Require(SceneManager.sceneCount == 1, "More than one level scene loaded.");
        Require(UnityEngine.Object.FindObjectsByType<MainMenuView>(FindObjectsInactive.Include).Length == 0, "MainMenu survived rollback.");
        Require(UnityEngine.Object.FindObjectsByType<Camera>().Length == 1, "Extra camera remains.");
        Require(UnityEngine.Object.FindObjectsByType<AudioListener>().Length == 1, "Extra AudioListener remains.");
        Require(UnityEngine.Object.FindObjectsByType<EventSystem>().Length <= 1, "Extra EventSystem remains.");
        return Inspect(label);
    }

    public static object ReloadEditorCode()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Reload in Edit Mode.");
        EditorUtility.RequestScriptReload();
        return new { requested = true };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
