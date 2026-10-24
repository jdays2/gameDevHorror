using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift.Features.ScriptedEncounters.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Validate the selective index payloads independently from the dirty working maps.
public static class VerifySceneGroupingCommit
{
    public static object Run()
    {
        if (Application.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Preserve the clean, single-map Editor setup first.");
        }
        const string recovery = "Assets/_Recovery/Task002CommitVerify/";
        const string work = "work/scene-grouping-task002/";
        if (Directory.Exists(recovery))
        {
            throw new InvalidOperationException("Do not overwrite an earlier commit verification.");
        }
        Directory.CreateDirectory(recovery);
        var results = new List<object>();
        foreach (string name in new[] { "MechanicsSandbox", "MorgueRouteTest" })
        {
            string oldPath = recovery + "Before-" + name + ".unity";
            string newPath = recovery + "After-" + name + ".unity";
            File.Copy(work + "index-base-" + name + ".unity", oldPath);
            File.Copy(work + "index-task-" + name + ".unity", newPath);
            AssetDatabase.ImportAsset(oldPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceSynchronousImport);
            Scene before = EditorSceneManager.OpenPreviewScene(oldPath);
            Scene after = EditorSceneManager.OpenPreviewScene(newPath);
            try
            {
                var original = All(before).ToDictionary(Key, value => value.localToWorldMatrix);
                var current = All(after).ToDictionary(Key);
                float maximum = 0;
                foreach (var pair in original)
                {
                    if (!current.TryGetValue(pair.Key, out Transform actual))
                    {
                        throw new InvalidOperationException("Committed original Transform missing: " + pair.Key);
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        maximum = Mathf.Max(maximum, Mathf.Abs(pair.Value[i] - actual.localToWorldMatrix[i]));
                    }
                }
                if (maximum > .00005f)
                {
                    throw new InvalidOperationException("Committed world pose changed: " + name);
                }
                int references = 0;
                foreach (Component component in All(after).SelectMany(value => value.GetComponents<Component>()))
                {
                    if (component == null)
                    {
                        throw new InvalidOperationException("Committed missing component: " + name);
                    }
                    using (var serialized = new SerializedObject(component))
                    {
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                        {
                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                references++;
                                if (property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default)
                                {
                                    throw new InvalidOperationException("Committed missing reference: " + component.name + "." + property.propertyPath);
                                }
                            }
                        }
                    }
                }
                var routes = All(after).Select(value => value.GetComponent<ScriptedStalkerRoute2D>()).Where(value => value != null).ToArray();
                if (name == "MorgueRouteTest" && (routes.Length != 4 || routes.Any(route => route.transform.parent.name != "Actors")
                    || routes.Select(route => route.TriggerDoor).Distinct().Count() != 1
                    || routes.Any(route => route.RoutePoints.Any(point => point == null || point.gameObject.scene != after))))
                {
                    throw new InvalidOperationException("Committed encounter is incomplete.");
                }
                results.Add(new { scene = name, originalTransforms = original.Count, afterTransforms = current.Count,
                    maximumWorldMatrixDelta = maximum, references, missingScripts = 0, missingReferences = 0, routes = routes.Length });
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(after);
                EditorSceneManager.ClosePreviewScene(before);
            }
        }
        File.WriteAllText("Docs/Validation/SceneGroupingTask002/commit-native-check.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        AssetDatabase.DeleteAsset(recovery.TrimEnd('/'));
        return results;
    }

    private static Transform[] All(Scene scene) => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

    private static string Key(Transform value)
    {
        GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(value);
        return id.targetObjectId + ":" + id.targetPrefabId;
    }
}
