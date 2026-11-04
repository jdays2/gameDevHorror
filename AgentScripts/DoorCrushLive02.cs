using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Doors.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit first-level opt-in and disposable native Play evidence; never rebuilds a scene.</summary>
public static class DoorCrushLive02
{
    private const string Level = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
    private const string Example = "Assets/EmergencyShift/Scenes/DoorCrushTest.unity";
    private const string Work = "work/door-crush02/";
    private const string Evidence = "Docs/Validation/DoorCrush02/";

    public static object Inspect()
    {
        return new
        {
            playing = EditorApplication.isPlaying,
            paused = EditorApplication.isPaused,
            compiling = EditorApplication.isCompiling,
            updating = EditorApplication.isUpdating,
            entry = EditorSceneManager.playModeStartScene,
            scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i =>
            {
                Scene scene = SceneManager.GetSceneAt(i);
                return new { scene.path, scene.isDirty, scene.isLoaded, doors = Doors(scene).Select(Describe).ToArray() };
            }).ToArray()
        };
    }

    public static object RememberSetup()
    {
        RequireCleanStopped();
        Write(Work + "editor-setup.json", EditorSceneManager.GetSceneManagerSetup());
        return Inspect();
    }

    public static object OpenLevel() => Open(Level);
    public static object OpenExample() => Open(Example);

    public static object RestoreSetup()
    {
        RequireCleanStopped();
        var setup = JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Work + "editor-setup.json"));
        EditorSceneManager.RestoreSceneManagerSetup(setup);
        return Inspect();
    }

    public static object RestoreOnlyMatchingSavedLevel()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Stopped Editor required.");
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == Level, "Inspect only the requested first level.");
        Scene preview = EditorSceneManager.OpenPreviewScene(Level);
        object result;
        try
        {
            var actual = SerializedScene(scene);
            var saved = SerializedScene(preview);
            string[] missing = saved.Keys.Except(actual.Keys).ToArray();
            string[] extra = actual.Keys.Except(saved.Keys).ToArray();
            string[] changed = saved.Keys.Intersect(actual.Keys).Where(key => saved[key] != actual[key]).ToArray();
            string[] unexpected = changed.Where(key => !KnownPlayPresentation(key, saved[key], actual[key])).ToArray();
            Write(Work + "memory-differences.json", changed.Select(key =>
            {
                JObject before = JObject.Parse(saved[key]);
                JObject after = JObject.Parse(actual[key]);
                return new { key, fields = before.Properties().Where(property => !JToken.DeepEquals(property.Value, after[property.Name]))
                    .Select(property => new { property.Name, before = property.Value, after = after[property.Name] }).ToArray() };
            }).ToArray());
            result = new { objects = saved.Count, missing, extra, changed, knownPlayPresentation = changed.Except(unexpected).ToArray(), unexpected };
            Write(Work + "memory-vs-saved.json", result);
            Require(missing.Length == 0 && extra.Length == 0 && unexpected.Length == 0,
                "Unsaved scene differs from disk; preserve it and inspect memory-vs-saved.json.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
        // Restore the current saved asset only when differences are limited to known Play-driven visual caches.
        EditorSceneManager.OpenScene(Level, OpenSceneMode.Single);
        return result;
    }

    public static object EnableFirstLevel()
    {
        RequireCleanStopped();
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == Level, "Open only the requested first level before authoring.");
        Door2D[] doors = Doors(scene);
        Require(doors.Length > 0, "No first-level doors found.");
        foreach (Door2D door in doors)
        {
            ValidateLeaf(door);
        }
        var original = All<Component>(scene).Where(item => item != null)
            .ToDictionary(item => item, item => EditorJsonUtility.ToJson(item));
        Write(Work + "doors-before.json", doors.Select(Describe).ToArray());
        File.Copy(Level, Work + "MorgueRouteTest.before.unity", true);
        int added = 0;
        foreach (Door2D door in doors)
        {
            if (door.GetComponent<DoorCrush2D>() == null)
            {
                Undo.AddComponent<DoorCrush2D>(door.gameObject);
                added++;
            }
            Require(door.GetComponent<DoorCrush2D>().enabled, "An existing opt-in was explicitly disabled: " + PathOf(door.transform));
        }
        foreach (Component item in All<Component>(scene).Where(item => item != null && original.ContainsKey(item)))
        {
            Require(EditorJsonUtility.ToJson(item) == original[item], "Existing component changed: " + PathOf(item.transform) + "/" + item.GetType().Name);
        }
        Require(EditorSceneManager.SaveScene(scene), "Saving the clean requested level failed.");
        var result = new
        {
            scene = scene.path,
            doors = doors.Length,
            added,
            originalComponentsPreserved = original.Count,
            enabled = doors.Count(door => door.GetComponent<DoorCrush2D>().enabled),
            missingScripts = All<Transform>(scene).Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)),
            entries = doors.Select(Describe).ToArray()
        };
        Write(Evidence + "first-level-authoring.json", result);
        return result;
    }

    public static Task<object> VerifyStalkerDoors() => VerifyDoors("ShadowStalker");
    public static Task<object> VerifySleeperDoors() => VerifyDoors("Sleeper");
    public static Task<object> ShowStalker() => Show("stalker");
    public static Task<object> ShowSleeper() => Show("sleeper");

    private static async Task<object> VerifyDoors(string species)
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(Application.isPlaying && scene.path == Level, "Requires ordinary Play of the saved first level.");
        LevelContext2D context = All<LevelContext2D>(scene).Single();
        foreach (EnemyHealth2D enemy in All<EnemyHealth2D>(scene))
        {
            enemy.gameObject.SetActive(false);
        }
        context.Player.SetHealthCapacity(100, 100);
        var rows = new List<object>();
        foreach (Door2D door in Doors(scene))
        {
            Require(door.GetComponent<DoorCrush2D>() != null, "Unconfigured door: " + door.name);
            door.RestoreState(false);
            Vector3 point = door.Blocker.bounds.center;
            door.RestoreState(true);
            context.Player.transform.position = point - door.transform.right * 1.1f;
            context.Player.GetComponent<Rigidbody2D>().position = context.Player.transform.position;
            foreach (CameraFollow2D camera in All<CameraFollow2D>(scene))
            {
                camera.SnapToTarget();
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
            GameObject actor = context.CreateInstance(prefab, point, Quaternion.identity);
            Freeze(actor);
            var health = actor.GetComponent<EnemyHealth2D>();
            int deaths = 0;
            health.Died += () => deaths++;
            door.Toggle();
            float deadline = Time.realtimeSinceStartup + 6;
            while (door.IsMoving && Time.realtimeSinceStartup < deadline)
            {
                await Task.Delay(20);
            }
            var gore = actor.GetComponent<GoreBurst2D>();
            int sounds = species == "ShadowStalker"
                ? actor.GetComponent<StalkerAudio2D>().PlaybackCount(StalkerAudioSet.Cue.Death)
                : actor.GetComponent<SleeperAudio2D>().DeathCount;
            rows.Add(new { door = PathOf(door.transform), species, health = health.Current, deaths, sounds,
                gore = gore.CaptureRemains() != null, closed = !door.IsOpen && !door.IsMoving });
            Write(Evidence + "first-level-" + species.ToLowerInvariant() + ".json", rows);
            Require(!health.IsAlive && deaths == 1 && sounds == 1 && gore.CaptureRemains() != null && !door.IsMoving,
                "Native closing contact failed at " + door.name);
            UnityEngine.Object.Destroy(actor);
            await Task.Delay(20);
        }
        Write(Evidence + "first-level-" + species.ToLowerInvariant() + ".json", rows);
        return rows;
    }

    private static async Task<object> Show(string species)
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(Application.isPlaying && scene.path == Example, "Requires ordinary Play of the saved DoorCrushTest.");
        Require(!EditorApplication.isPaused && Time.timeScale == 1, "Resume ordinary unpaused Play before recording.");
        using var viewport = new Viewport();
        float viewportDeadline = Time.realtimeSinceStartup + 3;
        while ((Screen.width != 1280 || Screen.height != 720) && Time.realtimeSinceStartup < viewportDeadline)
        {
            await Task.Delay(20);
        }
        Require(Screen.width == 1280 && Screen.height == 720, "The native capture viewport did not resize.");
        LevelContext2D context = All<LevelContext2D>(scene).Single();
        context.Player.SetHealthCapacity(100, 100);
        context.Flashlight.SetPower(false);
        EnemyHealth2D target = All<EnemyHealth2D>(scene).Single(item =>
            species == "stalker" ? item.GetComponent<ShadowStalker2D>() != null : item.GetComponent<Sleeper2D>() != null);
        Require(target.IsAlive, "Start a fresh Play session before repeating this subject.");
        float y = target.transform.position.y;
        Door2D door = Doors(scene).Single(item => item.GetComponent<DoorCrush2D>() != null && Mathf.Abs(item.transform.position.y - y) < 1);
        context.Player.transform.position = new Vector3(-1.1f, y, 0);
        context.Player.GetComponent<Rigidbody2D>().position = context.Player.transform.position;
        foreach (CameraFollow2D camera in All<CameraFollow2D>(scene))
        {
            camera.SnapToTarget();
        }
        int deaths = 0;
        target.Died += () => deaths++;
        string folder = Work + species + "/";
        Directory.CreateDirectory(folder);
        var frames = new List<object>();
        float start = Time.realtimeSinceStartup;
        float closedAt = -1;
        bool opened = false;
        bool closed = false;
        int index = 0;
        while (Time.realtimeSinceStartup - start < 12)
        {
            float elapsed = Time.realtimeSinceStartup - start;
            if (!opened && elapsed >= .8f)
            {
                opened = door.Interact(context.Player.GetComponent<PlayerInteraction>());
                Require(opened, "Opening interaction was rejected.");
                if (species == "sleeper")
                {
                    target.GetComponent<Sleeper2D>().ReceiveBulletHit(1, target.transform.position, Vector2.left);
                }
            }
            if (opened && !closed && !door.IsMoving && target.transform.position.x <= .55f)
            {
                closed = door.Interact(context.Player.GetComponent<PlayerInteraction>());
                Require(closed, "Closing interaction was rejected.");
                closedAt = elapsed;
            }
            string filename = index.ToString("D4") + ".png";
            ScreenCapture.CaptureScreenshot(folder + filename);
            frames.Add(new { filename, seconds = elapsed, alive = target.IsAlive, door.IsMoving, door.IsOpen });
            index++;
            if (closed && elapsed - closedAt > 3)
            {
                break;
            }
            await Task.Delay(50);
        }
        await Task.Delay(250);
        int sounds = species == "stalker"
            ? target.GetComponent<StalkerAudio2D>().PlaybackCount(StalkerAudioSet.Cue.Death)
            : target.GetComponent<SleeperAudio2D>().DeathCount;
        var result = new { species, scene = scene.path, opened, closed, closedAt, health = target.Current, deaths, sounds,
            gore = target.GetComponent<GoreBurst2D>().CaptureRemains() != null,
            settled = target.GetComponent<GoreBurst2D>().IsSettled,
            doorFinished = !door.IsOpen && !door.IsMoving, playerHealth = context.Player.Health,
            frameCount = frames.Count, method = "Native ScreenCapture in ordinary Play; real brain pursuit and Door.Interact; temporary player health 100" };
        Write(folder + "frames.json", frames);
        Write(Evidence + species + "-live.json", result);
        Require(opened && closed && !target.IsAlive && deaths == 1 && sounds == 1 && !door.IsMoving,
            "The real pursuit or closing contact did not complete: " + species);
        return result;
    }

    private static object Open(string path)
    {
        RequireCleanStopped();
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Type gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameView != null)
        {
            EditorWindow.GetWindow(gameView).Focus();
        }
        return Inspect();
    }

    private static void RequireCleanStopped()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating, "A stopped, ready Editor is required.");
        Require(Enumerable.Range(0, SceneManager.sceneCount).All(i => !SceneManager.GetSceneAt(i).isDirty),
            "Preserve unsaved scenes; do not save or discard them automatically.");
        Directory.CreateDirectory(Work);
    }

    private static void ValidateLeaf(Door2D door)
    {
        var leaf = door.Blocker as BoxCollider2D;
        var serialized = new SerializedObject(door);
        Vector3 offset = serialized.FindProperty("openOffset").vector3Value;
        Require(leaf != null && !leaf.isTrigger && leaf.edgeRadius == 0 && offset.y > 0 && offset.x == 0 && offset.z == 0,
            "Review unsupported door rather than altering its geometry: " + PathOf(door.transform));
        Vector3 x = leaf.transform.TransformVector(Vector3.right);
        Vector3 y = leaf.transform.TransformVector(Vector3.up);
        Require(x.magnitude > 0 && y.magnitude > 0 && Mathf.Abs(x.z) < .0001f && Mathf.Abs(y.z) < .0001f &&
            Mathf.Abs(Vector3.Dot(x.normalized, y.normalized)) < .0001f, "Unsupported skew/plane: " + door.name);
    }

    private static object Describe(Door2D door)
    {
        var serialized = new SerializedObject(door);
        var leaf = door.Blocker as BoxCollider2D;
        return new { path = PathOf(door.transform), active = door.gameObject.activeInHierarchy,
            crushing = door.GetComponent<DoorCrush2D>() != null && door.GetComponent<DoorCrush2D>().enabled,
            open = door.IsOpen, moving = door.IsMoving,
            position = Values(door.transform.position),
            rotation = new[] { door.transform.rotation.x, door.transform.rotation.y, door.transform.rotation.z, door.transform.rotation.w },
            scale = Values(door.transform.lossyScale),
            offset = Values(serialized.FindProperty("openOffset").vector3Value),
            duration = serialized.FindProperty("duration").floatValue,
            useAuthoredDuration = serialized.FindProperty("useAuthoredDuration").boolValue,
            boxSize = leaf != null ? new[] { leaf.size.x, leaf.size.y } : new[] { 0f, 0f },
            access = door.GetComponent<DoorAccess2D>() != null ? EditorJsonUtility.ToJson(door.GetComponent<DoorAccess2D>()) : null,
            prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(door.gameObject) };
    }

    private static void Freeze(GameObject actor)
    {
        foreach (Behaviour item in actor.GetComponents<Behaviour>())
        {
            if (item is ShadowStalker2D || item is Sleeper2D || item is CrowdAgent2D)
            {
                item.enabled = false;
            }
        }
        var body = actor.GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Kinematic;
        Physics2D.SyncTransforms();
    }

    private static Door2D[] Doors(Scene scene) => All<Door2D>(scene).OrderBy(item => PathOf(item.transform)).ToArray();
    private static float[] Values(Vector3 value) => new[] { value.x, value.y, value.z };
    private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    private static string PathOf(Transform item) => item.parent != null ? PathOf(item.parent) + "/" + item.name : item.name;
    private static string IdentityPath(Transform item) => (item.parent != null ? IdentityPath(item.parent) + "/" : "") +
        item.name + "[" + item.GetSiblingIndex() + "]";
    private static Dictionary<string, string> SerializedScene(Scene scene)
    {
        var result = new Dictionary<string, string>();
        UnityEngine.Object[] objects = All<Transform>(scene).Select(item => (UnityEngine.Object)item.gameObject)
            .Concat(All<Component>(scene).Where(item => item != null)).ToArray();
        foreach (UnityEngine.Object item in objects)
        {
            JToken json = JToken.Parse(EditorJsonUtility.ToJson(item));
            if (json is JObject wrapper && wrapper.Count == 1 && wrapper.Properties().First().Value is JObject body)
            {
                json = body;
            }
            var serialized = new SerializedObject(item);
            SerializedProperty property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }
                JToken token = json.SelectToken(property.propertyPath.Replace(".Array.data[", "["));
                if (token != null)
                {
                    token.Replace(new JValue(Identity(property.objectReferenceValue)));
                }
            }
            result.Add(Identity(item), json.ToString(Formatting.None));
        }
        return result;
    }
    private static bool KnownPlayPresentation(string key, string saved, string actual)
    {
        JObject before = JObject.Parse(saved);
        JObject after = JObject.Parse(actual);
        string[] fields = before.Properties().Where(property => !JToken.DeepEquals(property.Value, after[property.Name]))
            .Select(property => property.Name).ToArray();
        if (key.StartsWith("scene:Death Screen[") && key.Contains("/Menu Column[") && key.EndsWith("/UnityEngine.RectTransform:0"))
        {
            return fields.All(field => field == "m_AnchorMin" || field == "m_AnchorMax" || field == "m_SizeDelta");
        }
        if (key.Contains("/Start - small flickering ceiling light[0]/UnityEngine.Rendering.Universal.Light2D:0"))
        {
            return fields.All(field => field == "m_Enabled" || field == "m_Intensity") &&
                after["m_Enabled"].Value<bool>() == false && after["m_Intensity"].Value<float>() == 0;
        }
        return key.Contains("/Start - small flickering ceiling light[0]/Small overhead fixture[0]/Diffuser[0]/UnityEngine.SpriteRenderer:0") &&
            fields.All(field => field == "m_Enabled") && after["m_Enabled"].Value<bool>() == false;
    }
    private static string Identity(UnityEngine.Object item)
    {
        if (item == null)
        {
            return "null";
        }
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item, out string guid, out long local))
        {
            if (!string.IsNullOrEmpty(guid))
            {
                return "asset:" + guid + ":" + local;
            }
        }
        if (item is GameObject gameObject)
        {
            return "scene:" + IdentityPath(gameObject.transform);
        }
        if (item is Component component)
        {
            Component[] siblings = component.GetComponents(component.GetType());
            return "scene:" + IdentityPath(component.transform) + "/" + component.GetType().FullName + ":" + Array.IndexOf(siblings, component);
        }
        return item.GetType().FullName + ":" + item.name;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
    private static void Write(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
    }

    // Same native GameView API as the existing layout review; restore the user's selection and remove only our entry.
    private sealed class Viewport : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        private readonly Type viewType;
        private readonly Type sizeType;
        private readonly EditorWindow view;
        private readonly object group;
        private readonly int originalIndex;
        private readonly string label;

        public Viewport()
        {
            Assembly editor = typeof(Editor).Assembly;
            viewType = editor.GetType("UnityEditor.GameView");
            sizeType = editor.GetType("UnityEditor.GameViewSize");
            view = EditorWindow.GetWindow(viewType);
            originalIndex = (int)viewType.GetProperty("selectedSizeIndex", Flags).GetValue(view);
            object sizes = editor.GetType("UnityEditor.GameViewSizes").GetProperty("instance", Flags).GetValue(null);
            group = sizes.GetType().GetProperty("currentGroup", Flags).GetValue(sizes);
            label = "DoorCrush02-" + DateTime.UtcNow.Ticks;
            object entry = Activator.CreateInstance(sizeType, Flags, null, new object[] {
                Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), 1280, 720, label }, null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry });
            Select((int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1);
            view.Focus();
        }

        public void Dispose()
        {
            Select(originalIndex);
            int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            for (int index = total - 1; index >= 0; index--)
            {
                object entry = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { index });
                if ((string)sizeType.GetProperty("baseText", Flags).GetValue(entry) == label)
                {
                    group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
                }
            }
        }

        private void Select(int index)
        {
            viewType.GetMethod("SizeSelectionCallback", Flags).Invoke(view, new object[] { index, null });
            view.Repaint();
        }
    }
}
