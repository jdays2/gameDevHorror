using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.ScriptedEncounters.Config;
using EmergencyShift.Features.ScriptedEncounters.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IntroEncounterAuthoring
{
    private const string ScenePath = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
    private const string Evidence = "Temp/IntroRouteBaseline/";

    public static string PathOf(Transform value) => value.parent == null ? value.name : PathOf(value.parent) + "/" + value.name;
    private static float[] Position(Vector3 value) => new[] { value.x, value.y, value.z };

    public static object Inspect()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Intro authoring requires EditMode.");
        }

        Scene scene = SceneManager.GetActiveScene();
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var result = new
        {
            scene = scene.path,
            dirty = scene.isDirty,
            roots = scene.GetRootGameObjects().Select(root => root.name).ToArray(),
            actors = objects.Select(value => value.GetComponent<ShadowStalker2D>()).Where(value => value != null)
                .Select(value => new { path = PathOf(value.transform), position = Position(value.transform.position),
                    fields = EditorJsonUtility.ToJson(value), patrol = value.GetComponent<EnemyPatrol2D>() != null
                        ? EditorJsonUtility.ToJson(value.GetComponent<EnemyPatrol2D>()) : null }).ToArray(),
            doors = objects.Select(value => value.GetComponent<Door2D>()).Where(value => value != null)
                .Select(value => new { path = PathOf(value.transform), position = Position(value.transform.position) }).ToArray(),
            geometry = objects.SelectMany(value => value.GetComponents<BoxCollider2D>())
                .Where(value => value.gameObject.layer == 8).Select(value => new { path = PathOf(value.transform),
                    center = Position(value.bounds.center), size = Position(value.bounds.size) }).ToArray()
        };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "live-before.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        if (scene.path == ScenePath)
        {
            EditorSceneManager.SaveScene(scene, Path.GetFullPath(Evidence + "LiveBefore.unity"), true);
        }

        return new { result.scene, result.dirty, actors = result.actors.Length, result.roots };
    }

    public static object Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("Requires the preserved MorgueRouteTest scene in EditMode with its latest edits saved.");
        }

        var existing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ScriptedStalkerRoute2D>(true)).ToArray();
        if (existing.Length != 0)
        {
            throw new InvalidOperationException("Intro authoring is one-shot; inspect existing routes instead of duplicating them.");
        }

        const string profilePath = "Assets/EmergencyShift/Data/Encounters/IntroLightRetreat.asset";
        if (!AssetDatabase.IsValidFolder("Assets/EmergencyShift/Data/Encounters"))
        {
            AssetDatabase.CreateFolder("Assets/EmergencyShift/Data", "Encounters");
        }

        var profile = AssetDatabase.LoadAssetAtPath<ScriptedRouteProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<ScriptedRouteProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        Transform world = scene.GetRootGameObjects().Single(root => root.name == "10_World").transform;
        Transform corridor = world.Find("C02_ServiceCorridor");
        Transform gameplay = corridor.Find("Gameplay");
        if (gameplay == null)
        {
            gameplay = Create("Gameplay", corridor).transform;
        }

        GameObject levelGameplay = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "20_LevelGameplay");
        if (levelGameplay == null)
        {
            levelGameplay = Create("20_LevelGameplay", null);
        }

        Transform markers = levelGameplay.transform.Find("IntroLightRetreat_Route");
        if (markers == null)
        {
            markers = Create("IntroLightRetreat_Route", levelGameplay.transform).transform;
        }
        Vector2[] positions = { new Vector2(14, .35f), new Vector2(19, .1f), new Vector2(21.1f, -1.95f),
            new Vector2(24.8f, -1.95f), new Vector2(26.6f, -.55f) };
        string[] names = { "01_CorridorExit", "02_HallApproach", "03_BelowLeftBlock", "04_PastBlockCorner", "05_RestPosition" };
        var points = new Transform[positions.Length];
        for (int index = 0; index < positions.Length; index++)
        {
            points[index] = markers.Find(names[index]);
            if (points[index] == null)
            {
                points[index] = Create(names[index], markers).transform;
            }
            points[index].position = positions[index];
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        Transform pending = gameplay.Find("Intro Stalker - flashlight retreat");
        var enemy = pending != null ? pending.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (pending == null)
        {
            Undo.RegisterCreatedObjectUndo(enemy, "Place intro Stalker");
        }
        enemy.name = "Intro Stalker - flashlight retreat";
        enemy.transform.SetParent(gameplay, true);
        enemy.transform.position = new Vector3(5.4f, .35f, 0);
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        enemy.GetComponent<ShadowStalker2D>().Configure(context.Player.transform, context.Flashlight, 1 << 8);
        var patrol = new SerializedObject(enemy.GetComponent<EnemyPatrol2D>());
        patrol.FindProperty("patrolEnabled").boolValue = false;
        patrol.ApplyModifiedPropertiesWithoutUndo();
        var route = Undo.AddComponent<ScriptedStalkerRoute2D>(enemy);
        var fields = new SerializedObject(route);
        fields.FindProperty("encounterId").stringValue = "morgue-intro-light-retreat";
        fields.FindProperty("profile").objectReferenceValue = profile;
        SerializedProperty array = fields.FindProperty("routePoints");
        array.arraySize = points.Length;
        for (int index = 0; index < points.Length; index++)
        {
            array.GetArrayElementAtIndex(index).objectReferenceValue = points[index];
        }

        fields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.GetComponent<ShadowStalker2D>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.GetComponent<EnemyPatrol2D>());
        Physics2D.SyncTransforms();
        float radius = enemy.GetComponent<CircleCollider2D>().radius * Mathf.Max(enemy.transform.lossyScale.x, enemy.transform.lossyScale.y);
        foreach (Transform point in points)
        {
            if (Physics2D.OverlapCircle(point.position, radius, (1 << 8) | (1 << 9)) != null)
            {
                throw new InvalidOperationException("Route point obstructed: " + point.name);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = enemy;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.sceneLighting = false;
            SceneView.lastActiveSceneView.drawGizmos = true;
            SceneView.lastActiveSceneView.LookAt(new Vector3(16, 0, 0), Quaternion.identity, 16, true, true);
        }

        var result = new { scene = scene.path, actor = PathOf(enemy.transform), start = Position(enemy.transform.position),
            points = points.Select(point => new { point.name, position = Position(point.position) }).ToArray(), radius,
            profile = profilePath, count = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<ShadowStalker2D>(true).Length) };
        Directory.CreateDirectory("Docs/Validation/IntroEncounter01");
        File.WriteAllText("Docs/Validation/IntroEncounter01/placement.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static GameObject Create(string name, Transform parent)
    {
        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Author intro route");
        if (parent != null)
        {
            created.transform.SetParent(parent, false);
        }

        return created;
    }

    public static object Review()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("Review requires the authored scene in EditMode.");
        }

        var roots = scene.GetRootGameObjects();
        var routes = roots.SelectMany(root => root.GetComponentsInChildren<ScriptedStalkerRoute2D>(true)).ToArray();
        if (routes.Length != 1)
        {
            throw new InvalidOperationException("Expected exactly one intro route.");
        }

        var route = routes[0];
        var fields = new SerializedObject(route);
        var result = new
        {
            scene = scene.path,
            dirty = scene.isDirty,
            route = route.EncounterId,
            routeCount = routes.Length,
            actor = PathOf(route.transform),
            prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(route.gameObject),
            pointCount = route.RoutePoints.Length,
            pointsAssigned = route.RoutePoints.All(point => point != null && point.gameObject.scene == scene),
            profile = AssetDatabase.GetAssetPath(fields.FindProperty("profile").objectReferenceValue),
            patrol = route.GetComponent<EnemyPatrol2D>().PatrolEnabled,
            missingScripts = roots.Sum(root => root.GetComponentsInChildren<Transform>(true)
                .Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject)))
        };
        if (result.dirty || !result.pointsAssigned || result.missingScripts != 0 || result.patrol)
        {
            throw new InvalidOperationException(JsonConvert.SerializeObject(result));
        }

        Selection.activeGameObject = route.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.sceneLighting = false;
            SceneView.lastActiveSceneView.drawGizmos = true;
            SceneView.lastActiveSceneView.LookAt(new Vector3(16, 0, 0), Quaternion.identity, 16, true, true);
            SceneView.lastActiveSceneView.Repaint();
        }

        File.WriteAllText("Docs/Validation/IntroEncounter01/native-references.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    public static object RemoveCaptureImport()
    {
        const string path = "Assets/Docs/Validation/IntroEncounter01/scene-route.png";
        bool removed = AssetDatabase.DeleteAsset(path);
        const string folder = "Assets/Docs/Validation/IntroEncounter01";
        if (Directory.Exists(folder) && Directory.GetFileSystemEntries(folder).Length == 0)
        {
            AssetDatabase.DeleteAsset(folder);
        }

        return new { removed };
    }
}
