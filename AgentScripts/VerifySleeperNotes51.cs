using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// BUG-051 native authoring and controlled Play evidence. Never attached or saved as a component.
public static class VerifySleeperNotes51
{
    private const string ScenePath = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
    private const string Output = "Docs/Validation/SleeperNotes51/";

    public static object Before() => Inspect("native-before");
    public static object After() => Inspect("native-after");

    public static object IndexPreview()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Preview only in stopped Editor.");
        Scene preview = EditorSceneManager.OpenPreviewScene("work/bug051/index-task.unity");
        try
        {
            Transform[] nodes = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            int missingScripts = nodes.Sum(node => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject));
            int missingReferences = 0;
            foreach (Component component in nodes.SelectMany(node => node.GetComponents<Component>()).Where(component => component != null))
            {
                using (var data = new SerializedObject(component))
                {
                    SerializedProperty property = data.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && !property.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))
                        {
                            missingReferences++;
                        }
                    }
                }
            }
            var notes = nodes.Where(node => node.name == "Sleeper_BehaviorNote" || node.name == "Sleeper_TargetNote")
                .Select(node => new { path = PathOf(node), position = V(node.position), active = node.gameObject.activeInHierarchy, text = node.GetComponentInChildren<UnityEngine.UI.Text>(true).text }).ToArray();
            var actors = nodes.Select(node => node.GetComponent<Sleeper2D>()).Where(actor => actor != null)
                .Select(actor => new { path = PathOf(actor.transform), position = V(actor.transform.position), actor.AttacksEnabled, constraints = actor.GetComponent<Rigidbody2D>().constraints.ToString(), crowdEnabled = actor.GetComponent<CrowdAgent2D>().enabled, maximumHealth = actor.Definition.MaximumHealth, prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor) }).ToArray();
            var result = new { transformsIncludingInactive = nodes.Length, missingScripts, missingReferences, notes, actors, unsavedWorkingSceneChanged = false };
            Write("index-native", result);
            Require(missingScripts == 0 && missingReferences == 0 && notes.Length == 2 && actors.Length == 2, "Selective commit native references failed.");
            Require(notes.Single(note => note.path.EndsWith("Sleeper_BehaviorNote")).path == "10_World/R02_AccessTest/Notes/Sleeper_BehaviorNote", "Wrong owner in selective commit.");
            Require(nodes.Single(node => node.name == "Sleeper_BehaviorNote").position == new Vector3(35.5f, 14.2f, 0) && nodes.Single(node => node.name == "Sleeper_TargetNote").position == new Vector3(56.5f, 7.3f, 0), "Wrong note positions in selective commit.");
            return result;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    public static object PreserveDirty()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Requires stopped Editor.");
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, "Wrong active scene.");
        string copy = "work/bug051/live-copy-" + DateTime.UtcNow.Ticks + ".unity";
        Require(EditorSceneManager.SaveScene(scene, copy, true), "Could not preserve the in-memory scene.");
        string saved = File.ReadAllText(ScenePath).Replace("\r\n", "\n");
        string live = File.ReadAllText(copy).Replace("\r\n", "\n");
        bool equal = saved == live;
        string pattern = @"(--- !u!224 &(?:1061168067|1451442420)\n(?:(?!--- !u!).)*?m_AnchoredPosition:) [^\n]+";
        bool onlyOwnedPositions = Regex.Replace(saved, pattern, "$1 OWNED", RegexOptions.Singleline) == Regex.Replace(live, pattern, "$1 OWNED", RegexOptions.Singleline);
        var result = new { scene.isDirty, copy, byteIdenticalToSavedScene = equal, onlyOwnedPositions };
        Write("dirty-state", result);
        Require(equal || onlyOwnedPositions, "Preserved distinct unsaved data; review the copy before any save.");
        Require(EditorSceneManager.SaveScene(scene), "Could not clear the redundant dirty flag.");
        return result;
    }

    public static object Inspect(string label)
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath && SceneManager.sceneCount == 1, "Expected one open MechanicsSandbox.");
        Transform[] nodes = Nodes();
        var notes = nodes.Where(node => node.name == "Sleeper_BehaviorNote" || node.name == "Sleeper_TargetNote")
            .Select(node => new
            {
                path = PathOf(node),
                id = GlobalObjectId.GetGlobalObjectIdSlow(node.gameObject).ToString(),
                active = node.gameObject.activeInHierarchy,
                position = V(node.position),
                scale = V(node.lossyScale),
                rotation = V(node.eulerAngles),
                canvasMode = node.GetComponent<Canvas>().renderMode.ToString(),
                order = node.GetComponent<Canvas>().sortingOrder,
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(node.gameObject),
                text = node.GetComponentInChildren<UnityEngine.UI.Text>(true).text,
                fontSize = node.GetComponentInChildren<UnityEngine.UI.Text>(true).fontSize,
                components = node.GetComponentsInChildren<Component>(true).Select(c => c == null ? "MISSING" : c.GetType().FullName).ToArray()
            }).ToArray();
        var actors = nodes.Select(node => node.GetComponent<Sleeper2D>()).Where(actor => actor != null)
            .Select(actor => new
            {
                path = PathOf(actor.transform),
                id = GlobalObjectId.GetGlobalObjectIdSlow(actor.gameObject).ToString(),
                position = V(actor.transform.position),
                attacksEnabled = actor.AttacksEnabled,
                constraints = actor.GetComponent<Rigidbody2D>().constraints.ToString(),
                crowdEnabled = actor.GetComponent<CrowdAgent2D>().enabled,
                definition = AssetDatabase.GetAssetPath(actor.Definition),
                maximumHealth = actor.Definition.MaximumHealth,
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor),
                overrides = (PrefabUtility.GetPropertyModifications(actor.gameObject) ?? Array.Empty<PropertyModification>())
                    .Select(m => new { m.propertyPath, m.value, reference = m.objectReference == null ? null : GlobalObjectId.GetGlobalObjectIdSlow(m.objectReference).ToString() }).ToArray()
            }).ToArray();
        var ids = new List<string>();
        int missingScripts = 0;
        int missingReferences = 0;
        foreach (Transform node in nodes)
        {
            missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject);
            foreach (Component component in node.GetComponents<Component>().Where(c => c != null))
            {
                using (var data = new SerializedObject(component))
                {
                    SerializedProperty persistent = data.FindProperty("persistentId");
                    if (persistent != null && persistent.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(persistent.stringValue))
                    {
                        ids.Add(persistent.stringValue);
                    }
                    SerializedProperty property = data.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && !property.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))
                        {
                            missingReferences++;
                        }
                    }
                }
            }
        }
        var result = new
        {
            scene = scene.path,
            scene.isDirty,
            playing = Application.isPlaying,
            playModeStartScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            transformsIncludingInactive = nodes.Length,
            missingScripts,
            missingReferences,
            persistentIds = ids.OrderBy(id => id).ToArray(),
            duplicatePersistentIds = ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray(),
            notes,
            actors
        };
        Write(label, result);
        Require(missingScripts == 0 && missingReferences == 0 && ids.Distinct().Count() == ids.Count, "Scene reference or identity error.");
        return result;
    }

    public static object Fix()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Author only while stopped and ready.");
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, "Wrong active scene.");
        if (scene.isDirty)
        {
            PreserveDirty();
        }
        Transform[] nodes = Nodes();
        Transform behavior = nodes.Single(node => node.name == "Sleeper_BehaviorNote");
        Transform target = nodes.Single(node => node.name == "Sleeper_TargetNote");
        Transform roomNotes = nodes.Single(node => PathOf(node) == "10_World/R02_AccessTest/Notes");
        Require(behavior.gameObject.activeSelf && target.gameObject.activeSelf, "Keep both authored notes active.");
        Undo.SetTransformParent(behavior, roomNotes, "BUG-051: note belongs to the live Sleeper test");
        Undo.RecordObject(behavior, "BUG-051: separate live test note");
        SetPosition((RectTransform)behavior, new Vector2(35.5f, 14.2f));
        Undo.RecordObject(target, "BUG-051: label stationary target");
        SetPosition((RectTransform)target, new Vector2(56.5f, 7.3f));
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "Target scene save failed.");
        return After();
    }

    private static void SetPosition(RectTransform rect, Vector2 position)
    {
        using (var data = new SerializedObject(rect))
        {
            data.FindProperty("m_AnchoredPosition").vector2Value = position;
            data.ApplyModifiedProperties();
        }
    }

    public static object RangeView() => MoveForView(new Vector2(50, 12));
    public static object TargetView() => MoveForView(new Vector2(55.5f, 5.5f));
    public static object LiveView() => MoveForView(new Vector2(34, 11));

    public static async Task<object> Beam()
    {
        TargetView();
        await Task.Delay(250); // Let the authored range-health trigger process the runtime relocation.
        Transform[] nodes = Nodes();
        Sleeper2D target = nodes.Select(node => node.GetComponent<Sleeper2D>()).Single(actor => actor != null && !actor.AttacksEnabled);
        LevelContext2D context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        Require(target.State == Sleeper2D.BehaviourState.Sleeping && target.Health == 8, "Requires a fresh sleeping target.");
        Vector2 origin = target.transform.position;
        int heroHealth = context.Player.Health;
        int lunges = 0;
        Action onLunge = () => lunges++;
        target.AttackLunged += onLunge;
        try
        {
            context.Flashlight.Acquire();
            context.Flashlight.SetPower(true);
            context.Flashlight.AimAt(target.transform.position);
            await Task.Delay(3000);
            var result = new
            {
                state = target.State.ToString(),
                target.Health,
                target.HasAggro,
                travel = Vector2.Distance(origin, target.transform.position),
                lunges,
                heroHealthBefore = heroHealth,
                heroHealthAfter = context.Player.Health,
                target.GetComponent<SleeperAudio2D>().WakeCount,
                seconds = 3,
                method = "Real authored player flashlight aimed at the real stationary target in ordinary Play."
            };
            Write("stationary-beam", result);
            Require(target.State == Sleeper2D.BehaviourState.Hunting && target.Health == 8 && result.travel < .0001f && lunges == 0 && context.Player.Health == heroHealth, "Beam wake changed stationary behavior.");
            return result;
        }
        finally
        {
            target.AttackLunged -= onLunge;
        }
    }

    private static object MoveForView(Vector2 point)
    {
        Require(Application.isPlaying, "Requires ordinary Play of the open scene.");
        LevelContext2D context = Nodes().Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        context.Flashlight.SetPower(false);
        Rigidbody2D body = context.Player.GetComponent<Rigidbody2D>();
        body.position = point;
        body.linearVelocity = Vector2.zero;
        context.Player.transform.position = new Vector3(point.x, point.y, 0);
        Physics2D.SyncTransforms();
        Camera.main.GetComponent<CameraFollow2D>().SnapToTarget();
        return new { player = V(context.Player.transform.position), camera = V(Camera.main.transform.position), Camera.main.orthographicSize, Camera.main.aspect };
    }

    public static async Task<object> Stationary()
    {
        Require(Application.isPlaying, "Requires fresh ordinary MechanicsSandbox Play.");
        TargetView();
        await Task.Delay(250);
        Transform[] nodes = Nodes();
        Sleeper2D target = nodes.Select(node => node.GetComponent<Sleeper2D>()).Single(actor => actor != null && !actor.AttacksEnabled);
        LevelContext2D context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        context.Flashlight.SetPower(false);
        Vector2 origin = target.transform.position;
        int lunges = 0;
        int resolved = 0;
        Action onLunge = () => lunges++;
        Action<bool> onResolve = landed => resolved++;
        target.AttackLunged += onLunge;
        target.AttackResolved += onResolve;
        int health = target.Health;
        int heroHealth = context.Player.Health;
        string initialState = target.State.ToString();
        float maxTravel = 0;
        var states = new HashSet<string>();
        try
        {
            Require(health == 8 && target.State == Sleeper2D.BehaviourState.Sleeping, "Expected fresh 8 HP sleeping target.");
            target.ReceiveBulletHit(1, origin, Vector2.right);
            for (int sample = 0; sample < 45; sample++)
            {
                await Task.Delay(100);
                maxTravel = Mathf.Max(maxTravel, Vector2.Distance(origin, target.transform.position));
                states.Add(target.State.ToString());
            }
            int woundedHealth = target.Health;
            Require(target.HasAggro && target.State == Sleeper2D.BehaviourState.Hunting, "Damage must wake the real target.");
            Require(woundedHealth == 7 && maxTravel < .0001f && lunges == 0 && resolved == 0 && context.Player.Health == heroHealth, "Stationary target moved or attacked.");
            target.ReceiveBulletHit(7, origin, Vector2.right);
            await Task.Delay(1800);
            var remains = target.GetComponent<GoreBurst2D>().CaptureRemains();
            var result = new
            {
                initialState,
                initialHealth = health,
                woundedHealth,
                sampleSeconds = 4.5f,
                states,
                maximumTravel = maxTravel,
                lunges,
                resolved,
                heroHealthBefore = heroHealth,
                heroHealthAfter = context.Player.Health,
                finalState = target.State.ToString(),
                finalHealth = target.Health,
                target.GetComponent<SleeperAudio2D>().WakeCount,
                target.GetComponent<SleeperAudio2D>().DeathCount,
                remainsParts = remains.parts.Length,
                settled = target.GetComponent<GoreBurst2D>().IsSettled,
                constraints = target.GetComponent<Rigidbody2D>().constraints.ToString(),
                crowdEnabled = target.GetComponent<CrowdAgent2D>().enabled,
                method = "Controlled runtime relocation of hero; real authored target, ReceiveBulletHit, native physics, AI and death pipeline. No saved actor edits."
            };
            Write("stationary-play", result);
            Require(target.State == Sleeper2D.BehaviourState.Dead && result.DeathCount == 1 && result.remainsParts > 0, "Expected one normal target death with remains.");
            return result;
        }
        finally
        {
            target.AttackLunged -= onLunge;
            target.AttackResolved -= onResolve;
        }
    }

    private static Transform[] Nodes() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
    private static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    private static string PathOf(Transform node) => node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name;
    private static void Write(string label, object value)
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + label + ".json", JsonConvert.SerializeObject(value, Formatting.Indented));
    }
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
