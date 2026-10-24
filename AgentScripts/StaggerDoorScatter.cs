using System;
using System.IO;
using System.Linq;
using EmergencyShift.Features.ScriptedEncounters.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StaggerDoorScatter
{
    private const string Evidence = "Docs/Validation/DoorScatterDelay01/";
    private static readonly string[] Ids = { "morgue-intro-light-retreat", "morgue-door-scatter-hall-01", "morgue-door-scatter-hall-02", "morgue-door-scatter-hall-03" };
    private static readonly float[] Delays = { 0, .45f, .2f, .7f };

    public static object Before() => Capture("before");
    public static object After() => Capture("after");

    public static object SnapshotPendingScene()
    {
        Scene scene = RequireScene();
        string path = Evidence + "pending-scene.unity";
        if (!EditorSceneManager.SaveScene(scene, path, true))
        {
            throw new IOException("Could not preserve the pending Editor state.");
        }

        return new { path, scene.isDirty };
    }

    public static object PrepareTests()
    {
        Scene scene = RequireScene();
        SnapshotPendingScene();
        if (!File.ReadAllBytes(scene.path).SequenceEqual(File.ReadAllBytes(Evidence + "pending-scene.unity")))
        {
            throw new InvalidOperationException("Unrelated unsaved values must remain in the authoring scene.");
        }

        // Clear only a dirty flag whose serialized scene is byte-identical to disk.
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        return new { testHost = "temporary empty scene", authoringPreserved = true };
    }

    public static object Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Stop checks and preserve dirty scenes before restoring the authoring view.");
        }

        EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity", OpenSceneMode.Single);
        return Capture("after");
    }

    public static object Apply()
    {
        Scene scene = RequireScene();
        if (scene.isDirty)
        {
            throw new InvalidOperationException("Preserve unrelated unsaved scene edits before authoring delays.");
        }

        ScriptedStalkerRoute2D[] routes = Routes(scene);
        foreach (ScriptedStalkerRoute2D route in routes)
        {
            int index = Array.IndexOf(Ids, route.EncounterId);
            if (index < 0 || route.StartCondition != ScriptedRouteStartCondition.DoorOpened)
            {
                throw new InvalidOperationException("Unexpected route in the authored encounter.");
            }

            var serialized = new SerializedObject(route);
            serialized.FindProperty("startDelaySeconds").floatValue = Delays[index];
            serialized.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new IOException("Could not save the authored delay overrides.");
        }

        Selection.objects = routes.Select(value => (UnityEngine.Object)value.gameObject).ToArray();
        return Capture("authored");
    }

    private static object Capture(string label)
    {
        Scene scene = RequireScene();
        Transform[] objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        ScriptedStalkerRoute2D[] routes = Routes(scene);
        var report = new
        {
            scene = scene.path,
            dirty = scene.isDirty,
            play = EditorApplication.isPlaying,
            missing = objects.Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject)),
            transforms = objects.Select(value => new { id = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString(), path = PathOf(value), position = V(value.position), rotation = new[] { value.rotation.x, value.rotation.y, value.rotation.z, value.rotation.w }, scale = V(value.lossyScale), active = value.gameObject.activeSelf }).ToArray(),
            routes = routes.Select(route => new
            {
                id = route.EncounterId,
                path = PathOf(route.transform),
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(route.gameObject),
                serialized = EditorJsonUtility.ToJson(route),
                points = route.RoutePoints.Select(point => new { id = GlobalObjectId.GetGlobalObjectIdSlow(point).ToString(), path = PathOf(point), position = V(point.position) }).ToArray()
            }).ToArray()
        };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + label + ".json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { report.scene, report.dirty, report.play, report.missing, transformCount = objects.Length, routeCount = routes.Length, routes = routes.Select(route => new { route.EncounterId, path = PathOf(route.transform), delay = new SerializedObject(route).FindProperty("startDelaySeconds")?.floatValue ?? 0 }).ToArray() };
    }

    private static Scene RequireScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity" || SceneManager.sceneCount != 1)
        {
            throw new InvalidOperationException("Expected stopped Editor on the single current MorgueRouteTest scene.");
        }

        return scene;
    }

    private static ScriptedStalkerRoute2D[] Routes(Scene scene)
    {
        ScriptedStalkerRoute2D[] routes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ScriptedStalkerRoute2D>(true)).OrderBy(route => Array.IndexOf(Ids, route.EncounterId)).ToArray();
        if (routes.Length != 4 || routes.Select(route => route.EncounterId).Distinct().Count() != 4)
        {
            throw new InvalidOperationException("Expected four unique encounter participants.");
        }

        return routes;
    }

    private static string PathOf(Transform value) => value.parent == null ? value.name : PathOf(value.parent) + "/" + value.name;
    private static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
}
