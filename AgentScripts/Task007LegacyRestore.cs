using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Task007LegacyRestore
{
    private const string Evidence = "Docs/Validation/Task007WholeGenerated10/LegacyRestore";

    public static object Observe()
    {
        return State();
    }

    public static object Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            throw new InvalidOperationException("The Editor must be stopped and ready.");
        }
        JObject registry = JObject.Parse(File.ReadAllText("../.coordination/EmergencyShift/state.json"));
        if (!registry["leases"].Any(lease => (string)lease["resource"] == "game" &&
            (string)lease["owner"] == "01a19d95-c7c2-7572-a3f6-ac9f69524790"))
        {
            throw new InvalidOperationException("TASK-007 does not own the game lease.");
        }
        JObject before = State();
        var prefabs = new JArray();
        foreach (string species in new[] { "ShadowStalker", "Sleeper" })
        {
            string path = "Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CreatureSpriteAnimator2D animator = root.GetComponentsInChildren<CreatureSpriteAnimator2D>(true).Single();
                using (var serialized = new SerializedObject(animator))
                {
                    foreach (var expected in new[] { ("walkFrames", 8), ("recoilFrames", 4), ("attackFrames", 6), ("deathFrames", 4) })
                    {
                        SerializedProperty frames = serialized.FindProperty(expected.Item1);
                        if (frames == null || frames.arraySize != expected.Item2)
                        {
                            throw new InvalidOperationException("Unexpected original frame array: " + species + "/" + expected.Item1);
                        }
                        for (int index = 0; index < frames.arraySize; index++)
                        {
                            if (frames.GetArrayElementAtIndex(index).objectReferenceValue == null)
                            {
                                throw new InvalidOperationException("Missing original frame.");
                            }
                        }
                    }
                    var result = new JObject { ["path"] = path };
                    foreach (string field in new[] { "spriteSet", "attackSpriteSet" })
                    {
                        SerializedProperty property = serialized.FindProperty(field);
                        result[field + "Before"] = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                        property.objectReferenceValue = null;
                        result[field + "After"] = JValue.CreateNull();
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabs.Add(result);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        JObject after = State();
        var report = new JObject
        {
            ["scope"] = "Disconnect rejected posture/attack tables. Use existing legacy whole-sprite arrays: walk8, recoil4, attack6 references, death4. No runtime C#, gameplay configs, scenes or source art changed.",
            ["before"] = before,
            ["after"] = after,
            ["sceneStateIdentical"] = JToken.DeepEquals(before, after),
            ["prefabs"] = prefabs,
            ["sceneSaves"] = 0
        };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Path.Combine(Evidence, "restoration.json"), report.ToString());
        return report;
    }

    private static JObject State()
    {
        var scenes = new JArray();
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            scenes.Add(new JObject
            {
                ["path"] = scene.path,
                ["loaded"] = scene.isLoaded,
                ["active"] = scene == SceneManager.GetActiveScene(),
                ["dirty"] = scene.isDirty,
                ["roots"] = scene.rootCount
            });
        }
        return new JObject
        {
            ["playing"] = EditorApplication.isPlaying,
            ["compiling"] = EditorApplication.isCompiling,
            ["updating"] = EditorApplication.isUpdating,
            ["startScene"] = EditorSceneManager.playModeStartScene == null ? JValue.CreateNull() : new JValue(AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)),
            ["scenes"] = scenes
        };
    }
}
