using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinishLevelOrganization
{
    public static object RestoreLibraryLampViews()
    {
        string[] paths =
        {
            "Assets/EmergencyShift/Prefabs/Environment/HospitalLamp01.prefab",
            "Assets/EmergencyShift/Prefabs/World/MorgueCeilingLamp.prefab"
        };
        foreach (string path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var settings = prefab.GetComponent<EmergencyShift.LocalLampConfiguration2D>().Settings;
            var lamp = prefab.GetComponent<EmergencyShift.LocalLamp2D>();
            using (var serialized = new SerializedObject(lamp))
            {
                var source = (UnityEngine.Rendering.Universal.Light2D)serialized.FindProperty("source").objectReferenceValue;
                var bulb = (SpriteRenderer)serialized.FindProperty("bulb").objectReferenceValue;
                source.enabled = settings.startsOn;
                source.intensity = settings.intensity;
                bulb.enabled = settings.startsOn;
                EditorUtility.SetDirty(source);
                EditorUtility.SetDirty(bulb);
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
            }
        }
        return new { restoredFromExistingConfiguration = paths };
    }

    public static object Polish(string sceneName)
    {
        string path = "Assets/EmergencyShift/Scenes/" + sceneName + ".unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        if (scene.isDirty || Application.isPlaying)
        {
            throw new InvalidOperationException("Clean EditMode scene required.");
        }
        try
        {
            var before = new LevelAuthoringState(scene);
            if (sceneName == "MechanicsSandbox")
            {
                var overlap = scene.GetRootGameObjects().Single(root => root.name == "Blood overlap preview - five stationary targets");
                var world = scene.GetRootGameObjects().Single(root => root.name == "10_World");
                overlap.transform.SetParent(world.transform.Find("R03_WeaponsRange/Gameplay"), true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(overlap.transform);
            }
            int examples = LevelReuseLibrary.ConnectExamples(scene);
            var orderedRoots = scene.GetRootGameObjects().OrderBy(root => root.name == "Pause Menu" ? "99_Pause" : root.name).ToArray();
            for (int index = 0; index < orderedRoots.Length; index++)
            {
                orderedRoots[index].transform.SetSiblingIndex(index);
            }
            var result = new { scene = path, examples, preservation = before.AssertPreserved(),
                references = OrganizeLevelScenes.VerifyReferences(scene), roots = orderedRoots.Select(root => root.name).ToArray() };
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new IOException("Scene save failed.");
            }
            File.WriteAllText("work/level-reuse-20261022/" + sceneName + "-polish.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            if (opened && !scene.isDirty)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
