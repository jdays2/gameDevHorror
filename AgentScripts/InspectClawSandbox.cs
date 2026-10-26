using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectClawSandbox
{
    public static object OpenSandbox()
    {
        if (Application.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(index => SceneManager.GetSceneAt(index).isDirty))
        {
            throw new InvalidOperationException("Preserve Play/unsaved scenes before changing the editor setup.");
        }
        const string folder = "Docs/Validation/ClawSandbox01/";
        if (File.Exists(folder + "before.json") && !File.Exists(folder + "initial-editor.json"))
        {
            File.Copy(folder + "before.json", folder + "initial-editor.json");
        }
        EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/MechanicsSandbox.unity", OpenSceneMode.Single);
        return Capture();
    }

    public static object Capture()
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(index => SceneManager.GetSceneAt(index)).ToArray();
        var report = new
        {
            playMode = EditorApplication.isPlaying,
            startScene = EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : null,
            scenes = scenes.Select(scene => new
            {
                scene.name,
                scene.path,
                dirty = scene.isDirty,
                roots = scene.GetRootGameObjects().Select(root => Describe(root.transform)).ToArray()
            }).ToArray()
        };
        Directory.CreateDirectory("Docs/Validation/ClawSandbox01");
        File.WriteAllText("Docs/Validation/ClawSandbox01/before.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { report.playMode, report.startScene, scenes = scenes.Select(scene => new { scene.name, scene.path, dirty = scene.isDirty, objects = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length) }) };
    }

    private static object Describe(Transform node)
    {
        var sprite = node.GetComponent<SpriteRenderer>();
        return new
        {
            node.name,
            active = node.gameObject.activeSelf,
            position = V(node.position),
            rotation = V(node.eulerAngles),
            scale = V(node.lossyScale),
            prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(node.gameObject),
            components = node.GetComponents<Component>().Select(component => component != null ? component.GetType().FullName : "MISSING").ToArray(),
            sprite = sprite != null ? new { asset = AssetDatabase.GetAssetPath(sprite.sprite), bounds = V(sprite.bounds.size), sprite.sortingOrder, material = AssetDatabase.GetAssetPath(sprite.sharedMaterial) } : null,
            children = Enumerable.Range(0, node.childCount).Select(index => Describe(node.GetChild(index))).ToArray()
        };
    }

    private static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
}
