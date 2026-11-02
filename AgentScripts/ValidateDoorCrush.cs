using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Doors.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateDoorCrush
{
    private const string ScenePath = "Assets/EmergencyShift/Scenes/DoorCrushTest.unity";
    private const string Evidence = "Docs/Validation/DoorCrush01/";

    public static object RegisterExample()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stopped Editor required.");
        }
        // Explicit registration of this requested playable example only; no menu/catalogue synchronization.
        if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath))
        {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        }
        return EditorBuildSettings.scenes.Select(item => new { item.path, item.enabled }).ToArray();
    }

    public static object Authoring()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("Inspect the actual example scene in stopped Editor.");
        }
        var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var ids = transforms.Select(item => item.GetComponent<PersistentEntity2D>()).Where(item => item != null).ToArray();
        var doors = transforms.Select(item => item.GetComponent<Door2D>()).Where(item => item != null).ToArray();
        var context = transforms.Select(item => item.GetComponent<LevelContext2D>()).Single(item => item != null);
        bool targetLinks = transforms.Select(item => item.GetComponent<ShadowStalker2D>()).Where(item => item != null)
            .All(item => item.Player == context.Player.transform && item.LightSource == context.Flashlight) &&
            transforms.Select(item => item.GetComponent<Sleeper2D>()).Where(item => item != null)
            .All(item => item.Player == context.Player.transform && item.LightSource == context.Flashlight);
        int missing = transforms.Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
        if (missing != 0 || !targetLinks || ids.Any(item => string.IsNullOrWhiteSpace(item.Id)) || ids.Select(item => item.Id).Distinct().Count() != ids.Length)
        {
            throw new InvalidOperationException("Invalid scripts or scene identity.");
        }
        var report = new
        {
            scene = scene.path,
            scene.isDirty,
            roots = scene.GetRootGameObjects().Select(item => item.name).ToArray(),
            missing,
            targetLinks,
            signs = transforms.Select(item => item.GetComponent<TextMesh>()).Where(item => item != null)
                .Select(item => new { item.text, item.characterSize, prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item) }).ToArray(),
            identities = ids.Select(item => new { item.Id, item.name }).ToArray(),
            cameras = transforms.Count(item => item.GetComponent<Camera>() != null),
            listeners = transforms.Count(item => item.GetComponent<AudioListener>() != null),
            contexts = transforms.Count(item => item.GetComponent<LevelContext2D>() != null),
            temporaryProbes = transforms.SelectMany(item => item.GetComponents<MonoBehaviour>()).Where(item => item != null && item.GetType().Assembly.GetName().Name.Contains("Development")).Count(),
            doors = doors.Select(item => new
            {
                path = Hierarchy(item.transform),
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item),
                crushing = item.GetComponent<DoorCrush2D>() != null,
                xy = new[] { item.transform.position.x, item.transform.position.y },
                leafScale = new[] { item.Blocker.transform.lossyScale.x, item.Blocker.transform.lossyScale.y },
                collider = item.Blocker.GetType().Name
            }).ToArray(),
            scope = "Only DoorCrushTest and new variants authored; source byte comparison is reported separately."
        };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "authoring.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    public static object Traversal()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Traversal check requires disposable Play.");
        }
        Scene scene = SceneManager.GetActiveScene();
        var door = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Door2D>())
            .Single(item => item.GetComponent<DoorCrush2D>() == null);
        const int walls = 1 << 8;
        Vector2 start = new Vector2(-2.5f, 4.8f);
        door.RestoreState(false);
        Physics2D.SyncTransforms();
        bool closedBlocks = Physics2D.CircleCast(start, .4f, Vector2.up, 2.4f, walls).collider != null;
        door.RestoreState(true);
        Physics2D.SyncTransforms();
        bool openClear = Physics2D.CircleCast(start, .4f, Vector2.up, 2.4f, walls).collider == null;
        bool outsideBlocked = Physics2D.CircleCast(new Vector2(-1, 5), .4f, Vector2.right, 2, walls).collider != null;
        if (!closedBlocks || !openClear || !outsideBlocked)
        {
            throw new InvalidOperationException("Example traversal/boundary mismatch.");
        }
        var result = new { closedBlocks, openClear, outsideBlocked, radius = .4f, check = "Reference door and corridor gap, actual Physics2D" };
        File.WriteAllText(Evidence + "traversal.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static string Hierarchy(Transform item) => item.parent == null ? item.name : Hierarchy(item.parent) + "/" + item.name;
}
