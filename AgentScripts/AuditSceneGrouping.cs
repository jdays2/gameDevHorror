using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EmergencyShift.Features.ScriptedEncounters.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reviewable TASK-002 tooling. No generators, global save, unpack or prefab apply.
public static class AuditSceneGrouping
{
    private const string Evidence = "work/scene-grouping-task002/";

    public static object CaptureBefore() => Capture("native-before");
    public static object CaptureAfter() => Capture("native-after");

    private static object Capture(string phase)
    {
        RequireEditMode();
        string folder = Evidence + phase;
        if (Directory.Exists(folder))
        {
            throw new InvalidOperationException("Do not overwrite the evidence: " + folder);
        }

        Directory.CreateDirectory(folder);
        var loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var setup = loaded.Select(scene => new { scene.path, scene.name, scene.isDirty, scene.isLoaded,
            active = scene == SceneManager.GetActiveScene() }).ToArray();
        foreach (Scene scene in loaded.Where(scene => scene.isLoaded))
        {
            bool dirty = scene.isDirty;
            string copy = folder + "/live-" + scene.name + ".unity";
            if (dirty)
            {
                string recovery = "Assets/_Recovery/Task002/" + phase + "-" + scene.name + ".unity";
                Directory.CreateDirectory(Path.GetDirectoryName(recovery));
                if (!EditorSceneManager.SaveScene(scene, recovery, true) || scene.isDirty != dirty)
                {
                    throw new InvalidOperationException("Could not preserve the dirty live scene without changing its flag.");
                }
                File.Copy(recovery, copy);
            }
            else if (File.Exists(scene.path))
            {
                File.Copy(scene.path, copy);
            }
            Write(folder + "/live-" + scene.name + ".json", Inspect(scene));
        }

        var summaries = new List<object>();
        string[] paths = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => !path.StartsWith("Assets/_Recovery/Task002/", StringComparison.Ordinal))
            .OrderBy(path => path).ToArray();
        foreach (string path in paths)
        {
            // A preview prevents editor setup changes and never saves the asset.
            Scene preview = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var inspection = Inspect(preview);
                Write(folder + "/" + Path.GetFileNameWithoutExtension(path) + ".json", inspection);
                summaries.Add(new { scene = path, objects = All(preview).Length,
                    roots = preview.GetRootGameObjects().Select(root => root.name).ToArray() });
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        var result = new { setup, playModeStartScene = EditorSceneManager.playModeStartScene == null ? null
            : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), scenes = summaries };
        Write(folder + "/summary.json", result);
        return result;
    }

    private static object Inspect(Scene scene)
    {
        Transform[] objects = All(scene);
        var issues = new List<string>();
        var units = new List<object>();
        var fields = new SortedDictionary<string, string>();
        int components = 0;
        int references = 0;
        foreach (Transform value in objects)
        {
            string id = Id(value.gameObject);
            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(value.gameObject);
            string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(value.gameObject);
            if (prefabRoot == null || prefabRoot == value.gameObject)
            {
                Renderer[] renderers = value.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = new Bounds(value.position, Vector3.zero);
                foreach (Renderer renderer in renderers.Where(renderer => !(renderer is ParticleSystemRenderer)))
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                units.Add(new { id, path = PathOf(value), parent = value.parent == null ? null : Id(value.parent.gameObject),
                    name = value.name, position = Vec(value.position), scale = Vec(value.lossyScale),
                    matrix = Matrix(value.localToWorldMatrix), active = value.gameObject.activeSelf,
                    childCount = value.childCount, prefab,
                    prefabStatus = PrefabUtility.GetPrefabInstanceStatus(value.gameObject).ToString(),
                    components = value.GetComponents<Component>().Select(component => component == null ? "MISSING" : component.GetType().FullName).ToArray(),
                    bounds = new { center = Vec(bounds.center), size = Vec(bounds.size) } });
            }

            UnityEngine.Object[] owners = new UnityEngine.Object[] { value.gameObject }.Concat(value.GetComponents<Component>()).ToArray();
            foreach (UnityEngine.Object owner in owners)
            {
                if (owner == null)
                {
                    issues.Add("Missing component: " + PathOf(value));
                    continue;
                }
                components++;
                string ownerId = Id(owner);
                using (var serialized = new SerializedObject(owner))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            references++;
                            if (property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default)
                            {
                                issues.Add("Missing reference: " + PathOf(value) + "." + property.propertyPath);
                            }
                        }
                        string content = Value(property);
                        if (content != null)
                        {
                            fields[ownerId + "|" + property.propertyPath] = content;
                        }
                    }
                }
            }
        }

        var routes = objects.Select(value => value.GetComponent<ScriptedStalkerRoute2D>()).Where(value => value != null)
            .Select(value => new { id = Id(value), actorId = Id(value.gameObject), path = PathOf(value.transform),
                encounterId = value.EncounterId, startCondition = value.StartCondition.ToString(),
                door = value.TriggerDoor == null ? null : PathOf(value.TriggerDoor.transform),
                points = value.RoutePoints.Select(point => point == null ? null : new { id = Id(point), path = PathOf(point), position = Vec(point.position) }).ToArray() }).ToArray();
        string[] duplicateNames = objects.GroupBy(value => (value.parent == null ? "ROOT" : Id(value.parent)) + "/" + value.name)
            .Where(group => group.Count() > 1).Select(group => PathOf(group.First())).ToArray();
        var result = new { scene = scene.path, objects = objects.Length, components, references, issues,
            units, routes, duplicateNames, fields,
            dependencies = AssetDatabase.GetDependencies(scene.path, true).OrderBy(path => path).ToArray() };
        return result;
    }

    public static object FixMorgue()
    {
        RequireEditMode();
        const string path = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded || scene.isDirty)
        {
            throw new InvalidOperationException("Requires the preserved, clean current MorgueRouteTest. Do not overwrite dirty authoring.");
        }
        if (!File.Exists(Evidence + "native-before/summary.json"))
        {
            throw new InvalidOperationException("Capture the current native baseline first.");
        }

        Transform[] original = All(scene);
        ScriptedStalkerRoute2D[] routes = original.Select(value => value.GetComponent<ScriptedStalkerRoute2D>())
            .Where(value => value != null).OrderBy(value => value.EncounterId).ToArray();
        if (routes.Length != 4 || routes.Select(value => value.TriggerDoor).Distinct().Count() != 1)
        {
            throw new InvalidOperationException("Expected four routes sharing one authored door.");
        }

        // Capture every original object/component, including marker-only transforms.
        var poses = original.ToDictionary(value => value, value => value.localToWorldMatrix);
        var active = original.ToDictionary(value => value, value => value.gameObject.activeInHierarchy);
        var serialized = original.SelectMany(value => value.GetComponents<Component>())
            .Where(value => value != null && !(value is Transform)).ToDictionary(value => value, value => EditorJsonUtility.ToJson(value));
        var prefabLinks = original.ToDictionary(value => value, value => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(value.gameObject));
        var identities = original.ToDictionary(value => value, value => Id(value.gameObject));
        Transform gameplay = scene.GetRootGameObjects().Single(root => root.name == "20_LevelGameplay").transform;
        if (gameplay.Find("StartDoorScatter") != null)
        {
            throw new InvalidOperationException("Already grouped; inspect rather than apply twice.");
        }

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("TASK-002 group start-door encounter");
        var changes = new List<object>();
        try
        {
            Transform encounter = Create("StartDoorScatter", gameplay);
            Transform actors = original.Single(value => PathOf(value) == "10_World/C02_ServiceCorridor/Gameplay");
            if (actors.childCount != 1 || actors.GetChild(0).GetComponent<ScriptedStalkerRoute2D>() == null)
            {
                throw new InvalidOperationException("Do not move unrelated corridor gameplay into the encounter.");
            }
            Move(actors, encounter, "Actors", changes);
            Transform markers = gameplay.Find("StartDoorScatter_Routes");
            Move(markers, encounter, "Routes", changes);
            foreach (ScriptedStalkerRoute2D route in routes)
            {
                string role = route.EncounterId == "morgue-intro-light-retreat" ? "Intro" : "Hall" + route.EncounterId.Substring(route.EncounterId.Length - 2);
                Move(route.transform, actors, "Stalker_" + role, changes);
                Transform routeRoot = route.RoutePoints.First().parent;
                if (route.RoutePoints.Any(point => point.parent != routeRoot))
                {
                    throw new InvalidOperationException("Route markers must retain one complete route root.");
                }
                Move(routeRoot, markers, role + "_Route", changes);
            }

            foreach (Transform value in original)
            {
                if (value == null || Id(value.gameObject) != identities[value] || prefabLinks[value] != PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(value.gameObject))
                {
                    throw new InvalidOperationException("Original identity or prefab link changed.");
                }
                for (int index = 0; index < 16; index++)
                {
                    if (Mathf.Abs(poses[value][index] - value.localToWorldMatrix[index]) > .00005f)
                    {
                        throw new InvalidOperationException("World matrix changed: " + value.name);
                    }
                }
                if (active[value] != value.gameObject.activeInHierarchy)
                {
                    throw new InvalidOperationException("Active state changed: " + value.name);
                }
            }
            foreach (var pair in serialized)
            {
                if (EditorJsonUtility.ToJson(pair.Key) != pair.Value)
                {
                    throw new InvalidOperationException("Authored component data changed: " + pair.Key.name);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Write(Evidence + "morgue-changes.json", new { changes, originalTransforms = poses.Count,
                originalComponents = serialized.Count, worldMatricesPreserved = true, componentDataPreserved = true,
                identitiesPreserved = true, prefabLinksPreserved = true, saved = false });
            // Separate explicit save after reviewing the in-memory result.
            Undo.CollapseUndoOperations(undo);
            return new { changes, originalTransforms = poses.Count, originalComponents = serialized.Count, saved = false };
        }
        catch
        {
            Undo.RevertAllDownToGroup(undo);
            throw;
        }
    }

    public static object SaveMorgue()
    {
        RequireEditMode();
        Scene scene = SceneManager.GetSceneByPath("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity");
        Transform encounter = scene.GetRootGameObjects().Single(root => root.name == "20_LevelGameplay").transform.Find("StartDoorScatter");
        if (encounter == null || encounter.Find("Actors").childCount != 4 || encounter.Find("Routes").childCount != 4)
        {
            throw new InvalidOperationException("Incomplete encounter; do not save.");
        }
        return new { saved = EditorSceneManager.SaveScene(scene), scene.isDirty, scene.path };
    }

    public static object ApplyReviewedPlan(string sceneName)
    {
        RequireEditMode();
        var plans = JArray.Parse(File.ReadAllText("Docs/Validation/SceneGroupingTask002/authoring-plan.json"));
        var plan = plans.Single(row => Path.GetFileNameWithoutExtension((string)row["scene"]) == sceneName);
        string path = (string)plan["scene"];
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        if (scene.isDirty)
        {
            throw new InvalidOperationException("Do not save unrelated dirty authoring: " + path);
        }

        Transform[] original = All(scene);
        var byId = original.ToDictionary(value => Id(value.gameObject));
        var poses = original.ToDictionary(value => value, value => value.localToWorldMatrix);
        var active = original.ToDictionary(value => value, value => value.gameObject.activeInHierarchy);
        var data = original.SelectMany(value => value.GetComponents<Component>())
            .Where(value => value != null && !(value is Transform)).ToDictionary(value => value, value => EditorJsonUtility.ToJson(value));
        var links = original.ToDictionary(value => value, value => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(value.gameObject));
        var changes = new List<object>();
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("TASK-002 reviewed scene ownership");
        try
        {
            var groups = new Dictionary<string, Transform>();
            foreach (var group in plan["groups"])
            {
                string parent = (string)group["parent"];
                Transform owner = groups.TryGetValue(parent, out var created) ? created : byId[parent];
                string name = (string)group["name"];
                if (owner.Cast<Transform>().Any(child => child.name == name))
                {
                    throw new InvalidOperationException("Group already exists: " + name);
                }
                groups.Add((string)group["key"], Create(name, owner));
            }
            foreach (var move in plan["moves"])
            {
                Transform value = byId[(string)move["id"]];
                if (PathOf(value) != (string)move["before"])
                {
                    throw new InvalidOperationException("Authored path changed since review: " + value.name);
                }
                string parent = (string)move["parent"];
                Transform owner = value.parent;
                if (parent != null)
                {
                    owner = groups.TryGetValue(parent, out var created) ? created : byId[parent];
                }
                Move(value, owner, (string)move["name"] ?? value.name, changes);
            }
            foreach (Transform value in original)
            {
                for (int index = 0; index < 16; index++)
                {
                    if (Mathf.Abs(poses[value][index] - value.localToWorldMatrix[index]) > .00005f)
                    {
                        throw new InvalidOperationException("World transform changed: " + PathOf(value));
                    }
                }
                if (active[value] != value.gameObject.activeInHierarchy || links[value] != PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(value.gameObject))
                {
                    throw new InvalidOperationException("Active state or prefab link changed: " + value.name);
                }
            }
            foreach (var pair in data)
            {
                if (pair.Value != EditorJsonUtility.ToJson(pair.Key))
                {
                    throw new InvalidOperationException("Authored component data changed: " + pair.Key.name);
                }
            }
            Undo.CollapseUndoOperations(undo);
            EditorSceneManager.MarkSceneDirty(scene);
            var result = new { scene = path, changes, originalTransforms = original.Length, originalComponents = data.Count,
                preserved = true, saved = EditorSceneManager.SaveScene(scene) };
            Write(Evidence + sceneName + "-changes.json", result);
            return result;
        }
        catch
        {
            Undo.RevertAllDownToGroup(undo);
            throw;
        }
        finally
        {
            if (opened && !scene.isDirty)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    public static object RestoreWorkingSetup()
    {
        RequireEditMode();
        var baseline = JObject.Parse(File.ReadAllText(Evidence + "native-before/summary.json"));
        var setup = (JArray)baseline["setup"];
        if (setup.Count != 1 || (bool)setup[0]["isDirty"] || (string)setup[0]["name"] != "MorgueRouteTest")
        {
            throw new InvalidOperationException("This cleanup only applies to the captured clean single-map setup.");
        }
        var owned = JArray.Parse(File.ReadAllText("Docs/Validation/SceneGroupingTask002/authoring-plan.json"))
            .Select(row => (string)row["scene"]).ToArray();
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        foreach (Scene scene in scenes)
        {
            if (!owned.Contains(scene.path))
            {
                throw new InvalidOperationException("A scene outside this task appeared; preserve it: " + scene.path);
            }
            if (!File.Exists(Evidence + "native-after/live-" + scene.name + ".unity"))
            {
                throw new InvalidOperationException("Preserve the in-memory authored scene before cleanup: " + scene.path);
            }
        }
        // Our saved asset diffs have been reviewed separately. The retained dirty
        // recovery copies include delayed URP shadow-mesh cache regeneration; do
        // not serialize those derived caches over the authored override set.
        Scene restored = EditorSceneManager.OpenScene((string)setup[0]["path"], OpenSceneMode.Single);
        var result = new { restored.path, restored.isDirty, scenes = SceneManager.sceneCount,
            playModeStartScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            retainedRecovery = Evidence + "native-after/", savedOtherScenes = false };
        Write(Evidence + "restored-setup.json", result);
        return result;
    }

    public static object ShowEncounter()
    {
        RequireEditMode();
        Scene scene = SceneManager.GetSceneByPath("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity");
        Transform encounter = scene.GetRootGameObjects().Single(root => root.name == "20_LevelGameplay").transform.Find("StartDoorScatter");
        Selection.activeGameObject = encounter.gameObject;
        EditorGUIUtility.PingObject(encounter.gameObject);
        if (SceneView.lastActiveSceneView != null)
        {
            var view = SceneView.lastActiveSceneView;
            view.in2DMode = true;
            view.sceneLighting = false;
            view.drawGizmos = true;
            view.LookAt(new Vector3(5.9f, 0, 0), Quaternion.identity, 4.5f, true, true);
            view.Repaint();
        }
        return new { selected = PathOf(encounter), actors = encounter.Find("Actors").childCount,
            routes = encounter.Find("Routes").childCount, scene.isDirty };
    }

    public static object ArchiveOwnRecoveryCopies()
    {
        RequireEditMode();
        const string source = "Assets/_Recovery/Task002";
        const string destination = Evidence + "native-after/recovery-assets";
        string expected = Path.GetFullPath(source);
        if (!expected.StartsWith(Path.GetFullPath("Assets/_Recovery") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Recovery cleanup must stay within the task's fixed directory.");
        }
        Directory.CreateDirectory(destination);
        string[] files = Directory.GetFiles(source);
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            if (!name.StartsWith("native-after-", StringComparison.Ordinal) || !(name.EndsWith(".unity") || name.EndsWith(".unity.meta")))
            {
                throw new InvalidOperationException("Unexpected file in own recovery directory: " + file);
            }
            string target = Path.Combine(destination, name);
            File.Copy(file, target, false);
            if (!File.ReadAllBytes(file).SequenceEqual(File.ReadAllBytes(target)))
            {
                throw new IOException("Recovery copy verification failed: " + file);
            }
        }
        if (!AssetDatabase.DeleteAsset(source))
        {
            throw new IOException("Could not remove the verified task-only recovery assets.");
        }
        return new { archivedFiles = files.Length, destination, originalRecoveryUntouched = "Assets/_Recovery/0.unity" };
    }

    private static void Move(Transform value, Transform parent, string name, List<object> changes)
    {
        string before = PathOf(value);
        Undo.SetTransformParent(value, parent, true, "Group complete authored unit");
        Undo.RecordObject(value.gameObject, "Name encounter role");
        value.name = name;
        if (PrefabUtility.IsPartOfPrefabInstance(value))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(value);
            PrefabUtility.RecordPrefabInstancePropertyModifications(value.gameObject);
        }
        changes.Add(new { id = Id(value.gameObject), before, after = PathOf(value) });
    }

    private static Transform Create(string name, Transform parent)
    {
        var value = new GameObject(name);
        SceneManager.MoveGameObjectToScene(value, parent.gameObject.scene);
        value.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(value, "Create encounter group");
        return value.transform;
    }

    private static void RequireEditMode()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            throw new InvalidOperationException("Requires an idle EditMode Editor and exclusive authoring slot.");
        }
    }

    private static Transform[] All(Scene scene) => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
    private static string Id(UnityEngine.Object value) => GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
    private static string PathOf(Transform value) => value.parent == null ? value.name : PathOf(value.parent) + "/" + value.name;
    private static float[] Vec(Vector3 value) => new[] { value.x, value.y, value.z };
    private static float[] Matrix(Matrix4x4 value) => Enumerable.Range(0, 16).Select(index => value[index]).ToArray();
    private static void Write(string path, object value) => File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));

    private static string Value(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == null ? "null:" + property.objectReferenceEntityIdValue : Id(property.objectReferenceValue);
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character:
                return property.longValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.Float:
                return property.doubleValue.ToString("R", CultureInfo.InvariantCulture);
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();
            case SerializedPropertyType.String:
                return property.stringValue;
            default:
                return null;
        }
    }
}
