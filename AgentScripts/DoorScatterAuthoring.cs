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

public static class DoorScatterAuthoring
{
    private const string ScenePath = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
    private const string Baseline = "Temp/DoorScatterBaseline/";
    private const string Evidence = "Docs/Validation/DoorScatter01/";

    private static Scene RequireScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("Door scatter authoring requires the current MorgueRouteTest in EditMode.");
        }

        return scene;
    }

    private static Transform[] Objects(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

    private static float[] Position(Vector3 value) => new[] { value.x, value.y, value.z };
    private static string PathOf(Transform value) => value.parent == null ? value.name : PathOf(value.parent) + "/" + value.name;

    public static object Inspect()
    {
        Scene scene = RequireScene();
        if (Directory.Exists(Baseline))
        {
            throw new InvalidOperationException("Preserve the existing scatter baseline; do not overwrite it.");
        }

        Directory.CreateDirectory(Baseline);
        File.Copy(ScenePath, Baseline + "DiskBefore.unity");
        EditorSceneManager.SaveScene(scene, Path.GetFullPath(Baseline + "LiveBefore.unity"), true);
        var actors = Objects(scene).Select(value => value.GetComponent<ShadowStalker2D>()).Where(value => value != null)
            .Select(value => new { path = PathOf(value.transform), position = Position(value.transform.position),
                radius = value.GetComponent<CircleCollider2D>().radius * Mathf.Max(Mathf.Abs(value.transform.lossyScale.x), Mathf.Abs(value.transform.lossyScale.y)),
                fields = EditorJsonUtility.ToJson(value), prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(value.gameObject) }).ToArray();
        File.WriteAllText(Baseline + "actors.json", JsonConvert.SerializeObject(actors, Formatting.Indented));
        return new { scene = scene.path, dirty = scene.isDirty, actors };
    }

    public static object Apply()
    {
        Scene scene = RequireScene();
        if (!File.Exists(Baseline + "LiveBefore.unity"))
        {
            throw new InvalidOperationException("Inspect and preserve the live scene first.");
        }

        Transform[] objects = Objects(scene);
        var intro = objects.Select(value => value.GetComponent<ScriptedStalkerRoute2D>()).Single(value => value != null);
        Door2D door = objects.Single(value => PathOf(value) == "10_World/R01_Morgue/Gameplay/S-C0 - ordinary").GetComponent<Door2D>();
        var actors = new[] { intro.GetComponent<ShadowStalker2D>() }.Concat(Enumerable.Range(1, 3).Select(index => objects
            .Single(value => PathOf(value) == "10_World/R04_Hall/Gameplay/Additional Stalker 0" + index).GetComponent<ShadowStalker2D>())).ToArray();
        Vector2[] destinations = actors.Select(value => (Vector2)value.transform.position).ToArray();
        destinations[0] = intro.RoutePoints.Last().position;
        Vector2[] starts = { new Vector2(5.5f, -.7f), new Vector2(5.5f, .7f), new Vector2(7, .7f), new Vector2(7, -.7f) };
        Vector2[][] routes =
        {
            intro.RoutePoints.Select(value => (Vector2)value.position).ToArray(),
            new[] { new Vector2(14, .7f), new Vector2(17.6f, .7f), destinations[1] },
            new[] { new Vector2(14, .8f), new Vector2(18, 2.7f), destinations[2] },
            new[] { new Vector2(14, -.8f), new Vector2(19, -3), new Vector2(27, -3), destinations[3] }
        };
        routes[0][0] = new Vector2(14, -.75f);

        const string profilePath = "Assets/EmergencyShift/Data/Encounters/DoorScatter.asset";
        if (AssetDatabase.LoadAssetAtPath<ScriptedRouteProfile>(profilePath) != null)
        {
            throw new InvalidOperationException("Scatter is one-shot authoring; inspect existing scene instead of duplicating it.");
        }

        var profile = ScriptableObject.CreateInstance<ScriptedRouteProfile>();
        var profileFields = new SerializedObject(profile);
        profileFields.FindProperty("reactionSeconds").floatValue = 0;
        profileFields.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(profile, profilePath);
        Transform levelGameplay = scene.GetRootGameObjects().Single(root => root.name == "20_LevelGameplay").transform;
        Transform scatter = Create("StartDoorScatter_Routes", levelGameplay);
        for (int actorIndex = 0; actorIndex < actors.Length; actorIndex++)
        {
            ShadowStalker2D actor = actors[actorIndex];
            Undo.RecordObject(actor.transform, "Stage four Stalkers behind start door");
            actor.transform.position = starts[actorIndex];
            var route = actorIndex == 0 ? intro : Undo.AddComponent<ScriptedStalkerRoute2D>(actor.gameObject);
            Transform[] points;
            if (actorIndex == 0)
            {
                points = intro.RoutePoints;
                Undo.RecordObject(points[0], "Separate corridor approach lanes");
                points[0].position = routes[0][0];
            }
            else
            {
                Transform group = Create("HallStalker0" + actorIndex + "_Route", scatter);
                points = routes[actorIndex].Select((position, index) =>
                {
                    Transform point = Create((index + 1).ToString("D2") + (index == routes[actorIndex].Length - 1 ? "_OriginalPosition" : "_Passage"), group);
                    point.position = position;
                    return point;
                }).ToArray();
            }

            var fields = new SerializedObject(route);
            fields.FindProperty("encounterId").stringValue = actorIndex == 0 ? intro.EncounterId : "morgue-door-scatter-hall-0" + actorIndex;
            fields.FindProperty("profile").objectReferenceValue = profile;
            fields.FindProperty("startCondition").enumValueIndex = (int)ScriptedRouteStartCondition.DoorOpened;
            fields.FindProperty("triggerDoor").objectReferenceValue = door;
            SerializedProperty array = fields.FindProperty("routePoints");
            array.arraySize = points.Length;
            for (int index = 0; index < points.Length; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = points[index];
            }

            fields.ApplyModifiedProperties();
            var patrol = new SerializedObject(actor.GetComponent<EnemyPatrol2D>());
            patrol.FindProperty("patrolEnabled").boolValue = false;
            patrol.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.GetComponent<EnemyPatrol2D>());
        }

        Physics2D.SyncTransforms();
        Validate(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return Review();
    }

    public static object RefineLanes()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Refine lanes in EditMode.");
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(scene);
        Transform root = scene.GetRootGameObjects().Single(value => value.name == "20_LevelGameplay").transform;
        Transform introLane = root.Find("IntroLightRetreat_Route/01_CorridorExit");
        Transform nearSplit = root.Find("StartDoorScatter_Routes/HallStalker01_Route/02_Passage");
        Undo.RecordObjects(new UnityEngine.Object[] { introLane, nearSplit }, "Separate corridor approach lanes");
        introLane.position = new Vector3(14, -.75f, 0);
        nearSplit.position = new Vector3(17.6f, .7f, 0);
        Physics2D.SyncTransforms();
        Validate(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return Review();
    }

    public static object Review()
    {
        if (!Application.isPlaying)
        {
            Scene authored = SceneManager.GetSceneByPath(ScenePath);
            if (!authored.IsValid() || !authored.isLoaded)
            {
                authored = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            SceneManager.SetActiveScene(authored);
        }

        Scene scene = RequireScene();
        Validate(scene);
        var routes = Objects(scene).Select(value => value.GetComponent<ScriptedStalkerRoute2D>()).Where(value => value != null).ToArray();
        var result = new
        {
            scene = scene.path, dirty = scene.isDirty, actorCount = Objects(scene).Count(value => value.GetComponent<ShadowStalker2D>() != null),
            routes = routes.Select(route => new { id = route.EncounterId, actor = PathOf(route.transform),
                start = Position(route.transform.position), condition = route.StartCondition.ToString(), door = PathOf(route.TriggerDoor.transform),
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(route.gameObject),
                points = route.RoutePoints.Select(point => new { path = PathOf(point), position = Position(point.position) }).ToArray() }).ToArray()
        };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "placement.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        Selection.objects = routes.Select(route => (UnityEngine.Object)route.gameObject).ToArray();
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.sceneLighting = false;
            SceneView.lastActiveSceneView.drawGizmos = true;
            SceneView.lastActiveSceneView.LookAt(new Vector3(19, 0, 0), Quaternion.identity, 18, true, true);
            SceneView.lastActiveSceneView.Repaint();
            SceneView.lastActiveSceneView.Focus();
        }

        return result;
    }

    private static void Validate(Scene scene)
    {
        Transform[] objects = Objects(scene);
        var routes = objects.Select(value => value.GetComponent<ScriptedStalkerRoute2D>()).Where(value => value != null).ToArray();
        if (routes.Length != 4 || routes.Select(value => value.EncounterId).Distinct().Count() != 4 ||
            objects.Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject)) != 0)
        {
            throw new InvalidOperationException("Expected exactly four unique routes and no missing scripts.");
        }

        foreach (ScriptedStalkerRoute2D route in routes)
        {
            float radius = route.GetComponent<CircleCollider2D>().radius * Mathf.Max(Mathf.Abs(route.transform.lossyScale.x), Mathf.Abs(route.transform.lossyScale.y));
            if (route.StartCondition != ScriptedRouteStartCondition.DoorOpened || route.TriggerDoor == null || route.GetComponent<EnemyPatrol2D>().PatrolEnabled ||
                Physics2D.OverlapCircle(route.transform.position, radius, (1 << 8) | (1 << 9)) != null ||
                route.RoutePoints.Any(point => point == null || point.gameObject.scene != scene || Physics2D.OverlapCircle(point.position, radius, (1 << 8) | (1 << 9)) != null))
            {
                throw new InvalidOperationException("Invalid door, patrol, staging clearance or route: " + route.EncounterId);
            }
        }
    }

    private static Transform Create(string name, Transform parent)
    {
        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Author door scatter routes");
        created.transform.SetParent(parent, false);
        return created.transform;
    }
}
