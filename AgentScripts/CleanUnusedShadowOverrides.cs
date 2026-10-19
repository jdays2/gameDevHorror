using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CleanUnusedShadowOverrides
{
    private const string MissingGuid = "280b7908d23022545b84c141862fc18a";
    private static readonly string[] Paths =
    {
        "Assets/EmergencyShift/Scenes/BasementPrototype.unity",
        "Assets/EmergencyShift/Scenes/LevelTemplate.unity",
        "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity"
    };

    public static Task<object> InspectChanges()
    {
        return InspectChangesAt(Paths[0]);
    }

    public static Task<object> InspectTemplateChanges()
    {
        return InspectChangesAt(Paths[1]);
    }

    private static async Task<object> InspectChangesAt(string path)
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Inspection requires stopped Play.");
        }

        Scene active = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            await StabilizeDerivedState(scene);
            Dictionary<object, string> before = Capture(scene);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject[] instances = root.GetComponentsInChildren<Transform>(true)
                    .Where(item => PrefabUtility.IsOutermostPrefabInstanceRoot(item.gameObject))
                    .Select(item => item.gameObject).ToArray();
                if (instances.Length > 0)
                {
                    PrefabUtility.RemoveUnusedOverrides(instances, InteractionMode.AutomatedAction);
                }
            }

            await StabilizeDerivedState(scene);
            Dictionary<object, string> after = Capture(scene);
            return new { beforeCount = before.Count, afterCount = after.Count,
                changes = before.Where(pair => !after.TryGetValue(pair.Key, out string value) || value != pair.Value)
                    .Take(6).Select(pair => new { id = pair.Key.ToString(), before = pair.Value,
                        after = after.TryGetValue(pair.Key, out string value) ? value : "<object removed>" }).ToArray(),
                sceneSaved = false };
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(active);
        }
    }

    public static async Task<object> Apply()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Unused override cleanup requires stopped, compiled authoring state.");
        }

        Scene active = SceneManager.GetActiveScene();
        bool activeWasDirty = active.isDirty;
        string[] targets = Paths.Where(path => File.ReadAllText(path).Contains(MissingGuid)).ToArray();
        foreach (string path in targets)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                throw new InvalidOperationException("The targeted scene is already open; its live state was left intact: " + path);
            }
        }

        string backup = Path.Combine(Directory.GetCurrentDirectory(), "work", "unused-shadow-overrides-before");
        Directory.CreateDirectory(backup);
        foreach (string path in targets)
        {
            string saved = Path.Combine(backup, Path.GetFileName(path));
            if (File.Exists(saved))
            {
                if (!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(saved)))
                {
                    throw new InvalidOperationException("A targeted scene changed since the preserved backup: " + path);
                }
            }
            else
            {
                File.Copy(path, saved);
            }
        }

        var results = new List<object>();
        foreach (string path in targets)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                await StabilizeDerivedState(scene);
                Dictionary<object, string> before = Capture(scene);
                var instances = new List<GameObject>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (PrefabUtility.IsOutermostPrefabInstanceRoot(item.gameObject))
                        {
                            instances.Add(item.gameObject);
                        }
                    }
                }

                PrefabUtility.RemoveUnusedOverrides(instances.ToArray(), InteractionMode.AutomatedAction);
                await StabilizeDerivedState(scene);
                Dictionary<object, string> after = Capture(scene);
                if (before.Count != after.Count || before.Any(pair => !after.TryGetValue(pair.Key, out string value) || value != pair.Value))
                {
                    throw new InvalidOperationException("Cleanup changed effective component state; scene was not saved: " + path);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Could not save the verified metadata cleanup: " + path);
                }

                string text = File.ReadAllText(path);
                bool removedMissingReference = !text.Contains(MissingGuid);
                results.Add(new { scene = path, objectStatesCompared = before.Count,
                    prefabInstances = instances.Count, effectiveStateUnchanged = true, removedMissingReference });
                if (!removedMissingReference)
                {
                    throw new InvalidOperationException("The missing GUID was not removed by the official unused-overrides API: " + path);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded)
                {
                    SceneManager.SetActiveScene(active);
                }
            }
        }

        return new { scenes = results, backup, activeScene = active.path,
            activeSceneSaved = false, activeDirtyPreserved = active.isDirty == activeWasDirty };
    }

    private static Dictionary<object, string> Capture(Scene scene)
    {
        var result = new Dictionary<object, string>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                result.Add("GameObject:" + item.gameObject.GetEntityId(), EditorJsonUtility.ToJson(item.gameObject));
            }

            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    throw new InvalidOperationException("Missing scripts require a separate repair before metadata cleanup.");
                }

                string state = EditorJsonUtility.ToJson(component);
                if (component.GetType().FullName == "UnityEngine.Rendering.Universal.ShadowCaster2D" ||
                    component.GetType().FullName == "UnityEngine.Rendering.Universal.Light2D")
                {
                    state = NormalizeProviderSelectionCache(state);
                }

                if (component is Transform transform)
                {
                    state += "|sibling=" + transform.GetSiblingIndex();
                    Canvas canvas = transform is RectTransform ? transform.GetComponentInParent<Canvas>(true) : null;
                    if (canvas == null || canvas.rootCanvas.renderMode == RenderMode.WorldSpace)
                    {
                        state += "|world=" + transform.position.ToString("R") + "|rotation=" + transform.rotation.ToString("R") +
                            "|scale=" + transform.lossyScale.ToString("R");
                    }
                    // Screen-space UI is compared by every serialized RectTransform/Canvas
                    // value above; its inactive display matrix is derived by the renderer.
                }

                // Live object identity is only a comparison key for this operation, never a save ID.
                result.Add(component.GetEntityId(), state);
            }
        }

        return result;
    }

    private static async Task StabilizeDerivedState(Scene scene)
    {
        await Task.Delay(100);
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Authoring mode changed during inspection; nothing was saved.");
        }

        Canvas.ForceUpdateCanvases();
        // LocalLamp.OnValidate applies planar defaults; the ExecuteAlways ceiling
        // adapter normally reapplies its derived cookie on the following editor update.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CeilingLightOcclusion2D ceiling in root.GetComponentsInChildren<CeilingLightOcclusion2D>(true))
            {
                if (ceiling.isActiveAndEnabled)
                {
                    ceiling.SetCeilingLighting(ceiling.CeilingLighting);
                }
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private static string NormalizeProviderSelectionCache(string state)
    {
        JObject root = JObject.Parse(state);
        var behaviour = (JObject)root["MonoBehaviour"];
        var references = behaviour?["references"] as JObject;
        var entries = references?["RefIds"] as JArray;
        if (entries == null)
        {
            return state;
        }

        // URP Provider2DSources.UpdateCache rebuilds this Inspector candidate cache.
        // Keep all selected providers, their data and the actual ShadowMesh geometry.
        var byId = entries.ToDictionary(entry => (long)entry["rid"]);
        foreach (JToken entry in entries)
        {
            string type = (string)entry["type"]?["class"];
            if (type == "Shadow2DProviderSources" || type == "Light2DProviderSources")
            {
                ((JObject)entry["data"]).Remove("m_Provider2DCache");
            }
        }

        behaviour.Remove("references");
        var pending = new Queue<long>();
        var reachable = new HashSet<long>();
        EnqueueReferences(behaviour, pending);
        while (pending.Count > 0)
        {
            long id = pending.Dequeue();
            if (reachable.Add(id) && byId.TryGetValue(id, out JToken entry))
            {
                EnqueueReferences(entry["data"], pending);
            }
        }

        references["RefIds"] = new JArray(byId.Where(pair => pair.Key <= 0 || reachable.Contains(pair.Key))
            .OrderBy(pair => pair.Key).Select(pair => pair.Value));
        behaviour["references"] = references;
        return root.ToString(Formatting.None);
    }

    private static void EnqueueReferences(JToken token, Queue<long> pending)
    {
        if (token is JObject value && value["rid"] is JValue id && id.Type == JTokenType.Integer && (long)id > 0)
        {
            pending.Enqueue((long)id);
        }

        if (token is JContainer container)
        {
            foreach (JToken child in container.Children())
            {
                EnqueueReferences(child, pending);
            }
        }
    }
}
