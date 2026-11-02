using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Doors.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Creates only a new isolated mechanic example. Refuses to overwrite any existing scene/variant.</summary>
public static class CreateDoorCrushTest
{
    public const string ScenePath = "Assets/EmergencyShift/Scenes/DoorCrushTest.unity";
    private const string Root = "Assets/EmergencyShift/";
    private const string DoorPrefab = Root + "Prefabs/World/CrushingDoor.prefab";
    private const string WallPrefab = Root + "Prefabs/World/CrushTestPartition.prefab";
    private const string SignPrefab = Root + "Prefabs/World/CrushTestSign.prefab";

    public static object Create()
    {
        if (EditorApplication.isPlaying || File.Exists(ScenePath) || File.Exists(DoorPrefab) || File.Exists(WallPrefab) || File.Exists(SignPrefab))
        {
            throw new InvalidOperationException("Stopped Editor and new output paths required; edit existing assets instead of regenerating them.");
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("Preserve unsaved scenes before opening the example.");
            }
        }

        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var door = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/World/Door.prefab"), preview);
            door.transform.position = Vector3.zero;
            door.AddComponent<DoorCrush2D>();
            PrefabUtility.SaveAsPrefabAsset(door, DoorPrefab);
            var wall = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Environment/LevelSurfaces/FullHeightBlock-460x180.prefab"), preview);
            wall.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            wall.transform.localScale = Vector3.one;
            foreach (Transform child in wall.transform)
            {
                child.gameObject.SetActive(false);
            }
            PrefabUtility.SaveAsPrefabAsset(wall, WallPrefab);
            CreateSign(preview);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        Transform systems = Group(null, "00_Systems");
        GameObject rig = Spawn(Root + "Prefabs/Levels/LevelRig.prefab", systems, "LevelRig", Vector2.zero);
        var context = rig.GetComponentInChildren<LevelContext2D>(true);
        context.Player.gameObject.AddComponent<PlayerAccessCards2D>();
        context.Player.transform.position = new Vector3(-3, 0, 0);
        PrefabUtility.RecordPrefabInstancePropertyModifications(context.Player.transform);
        var playerSave = context.Player.gameObject.GetComponent<PlayerCheckpoint2D>() ?? context.Player.gameObject.AddComponent<PlayerCheckpoint2D>();
        playerSave.Configure(AssetDatabase.LoadAssetAtPath<WeaponCatalog>(Root + "Data/Levels/Weapons.asset"));
        Identity(context.Player.gameObject, "crush-example-player");
        systems.gameObject.AddComponent<SandboxSession2D>().Configure(context.Player);
        Transform world = Group(null, "10_World");
        Transform corridor = Group(world, "C01_ObservationCorridor");
        Floor(corridor, "Corridor floor", new Vector2(-2.5f, 5), new Vector2(5, 18));
        Wall(corridor, "West boundary", new Vector2(-5, 5), new Vector2(.3f, 18));
        Wall(corridor, "North boundary", new Vector2(-2.5f, 14), new Vector2(5, .3f));
        Wall(corridor, "South boundary", new Vector2(-2.5f, -4), new Vector2(5, .3f));
        Wall(corridor, "East boundary between rooms", new Vector2(0, 5), new Vector2(.3f, 2));
        Wall(corridor, "Reference doorway west jamb", new Vector2(-4.5f, 6), new Vector2(1, .3f));
        Wall(corridor, "Reference doorway east jamb", new Vector2(-.5f, 6), new Vector2(1, .3f));
        Room(world, "R01_Stalker", 0, "ShadowStalker", "STALKER");
        Room(world, "R02_Sleeper", 10, "Sleeper", "SLEEPER");
        Label(corridor, "LURE THROUGH DOOR\nE: OPEN / CLOSE\nR: FRESH TEST", new Vector2(-2.7f, -2.4f));
        // A regular reference door stays opt-out, including in this example.
        var regular = Spawn(Root + "Prefabs/World/Door.prefab", corridor, "Ordinary reference door - no crushing", new Vector2(-2.5f, 6));
        regular.transform.rotation = Quaternion.Euler(0, 0, 90);
        PrefabUtility.RecordPrefabInstancePropertyModifications(regular.transform);
        regular.AddComponent<DoorCheckpoint2D>();
        Identity(regular, "crush-example-ordinary-door");
        Label(corridor, "ORDINARY DOOR", new Vector2(-2.6f, 5.3f));
        // A modest inspection level keeps the moment of contact and remains readable.
        var ambient = rig.GetComponentsInChildren<Light2D>(true).First(light => light.lightType == Light2D.LightType.Global);
        ambient.intensity = .32f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ambient);
        ConfigureSubjects(scene, context);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        foreach (Scene other in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray())
        {
            if (other != scene)
            {
                EditorSceneManager.CloseScene(other, true);
            }
        }
        return new { scene = ScenePath, crushingVariant = DoorPrefab, partitionVariant = WallPrefab, authoredObjects = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length) };
    }

    private static void ConfigureSubjects(Scene scene, LevelContext2D context)
    {
        foreach (ShadowStalker2D enemy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShadowStalker2D>(true)))
        {
            enemy.Configure(context.Player.transform, context.Flashlight, enemy.BlockingGeometry);
            enemy.transform.position = new Vector3(2, enemy.transform.position.y, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
        }
        foreach (Sleeper2D enemy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Sleeper2D>(true)))
        {
            enemy.Configure(context.Player.transform, context.Flashlight, enemy.BlockingGeometry);
            enemy.transform.position = new Vector3(2, enemy.transform.position.y, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
        }
    }

    private static void CreateSign(Scene preview)
    {
        var obj = new GameObject("CrushTestSign", typeof(TextMesh));
        SceneManager.MoveGameObjectToScene(obj, preview);
        var label = obj.GetComponent<TextMesh>();
        label.text = "ROOM";
        label.fontSize = 48;
        label.characterSize = .055f;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(.6f, .68f, .7f);
        obj.GetComponent<MeshRenderer>().sortingOrder = -2;
        PrefabUtility.SaveAsPrefabAsset(obj, SignPrefab);
    }

    private static void Room(Transform world, string name, float y, string species, string label)
    {
        Transform room = Group(world, name);
        Transform geometry = Group(room, "Geometry");
        Floor(geometry, "Inspection room floor", new Vector2(4, y), new Vector2(8, 8));
        Wall(geometry, "East wall", new Vector2(8, y), new Vector2(.3f, 8));
        Wall(geometry, "North wall", new Vector2(4, y + 4), new Vector2(8, .3f));
        Wall(geometry, "South wall", new Vector2(4, y - 4), new Vector2(8, .3f));
        Wall(geometry, "West wall below entrance", new Vector2(0, y - 2.75f), new Vector2(.3f, 2.5f));
        Wall(geometry, "West wall above entrance - leaf pocket", new Vector2(0, y + 2.75f), new Vector2(.3f, 2.5f));
        Transform gameplay = Group(room, "Gameplay");
        var door = Spawn(DoorPrefab, gameplay, "Lift door - crush enabled", new Vector2(0, y));
        door.AddComponent<DoorCheckpoint2D>();
        Identity(door, name + "-door");
        var actor = Spawn(Root + "Prefabs/Enemies/" + species + ".prefab", gameplay, label + " - lure subject", new Vector2(2, y));
        if (species == "ShadowStalker")
        {
            actor.AddComponent<StalkerCheckpoint2D>();
        }
        else
        {
            actor.AddComponent<SleeperCheckpoint2D>();
        }
        Identity(actor, name + "-subject");
        Transform dressing = Group(room, "Dressing - feeding traces");
        Spawn(Root + "Prefabs/Environment/BloodTraces05/m01-oblique.prefab", dressing, "Feeding smear", new Vector2(4.5f, y + 1.6f));
        Spawn(Root + "Prefabs/Environment/MedicalRemains01/crushed-fragment.prefab", dressing, "Feeding remains", new Vector2(4.8f, y + 1.7f));
        Label(room, label, new Vector2(3.6f, y - 2.7f));
    }

    private static void Identity(GameObject obj, string id) => (obj.GetComponent<PersistentEntity2D>() ?? obj.AddComponent<PersistentEntity2D>()).AssignId(id);
    private static Transform Group(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }
    private static GameObject Spawn(string path, Transform parent, string name, Vector2 point)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        obj.name = name;
        obj.transform.position = point;
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        return obj;
    }
    private static void Wall(Transform parent, string name, Vector2 point, Vector2 size)
    {
        var obj = Spawn(WallPrefab, parent, name, point);
        obj.transform.localScale = new Vector3(size.x, size.y, 1);
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
    }
    private static void Floor(Transform parent, string name, Vector2 point, Vector2 size)
    {
        var obj = Spawn(Root + "Prefabs/Environment/LevelSurfaces/Floor-porcelain-ivory-Tiled.prefab", parent, name, point);
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.size = size;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }
    private static void Label(Transform parent, string text, Vector2 point)
    {
        var obj = Spawn(SignPrefab, parent, "Sign - " + text.Split('\n')[0], point);
        obj.transform.position = new Vector3(point.x, point.y, -.03f);
        var label = obj.GetComponent<TextMesh>();
        label.text = text;
        if (text.Contains("\n"))
        {
            label.characterSize = .04f;
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
    }
}
