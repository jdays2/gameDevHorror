using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit controlled Play evidence; never saves runtime changes or rebuilds a map.</summary>
public static class DoorCrushReview
{
    private const string Evidence = "Docs/Validation/DoorCrush01/";
    private const string Root = "Assets/EmergencyShift/";

    public static object EditorState()
    {
        return new
        {
            scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i =>
            {
                Scene scene = SceneManager.GetSceneAt(i);
                return new { scene.path, scene.isDirty, scene.isLoaded };
            }).ToArray(),
            entry = EditorSceneManager.playModeStartScene,
            playing = EditorApplication.isPlaying
        };
    }

    public static object RestoreEditor()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || SceneManager.sceneCount != 1 ||
            scene.path != "Assets/EmergencyShift/Scenes/DoorCrushTest.unity" || scene.isDirty)
        {
            throw new InvalidOperationException("Preserve current Editor state; expected only the clean owned example scene.");
        }
        EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/MechanicsSandbox.unity", OpenSceneMode.Single);
        return EditorState();
    }

    public static async Task<object> Baseline()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires an ordinary disposable Play session.");
        }

        Scene scene = SceneManager.GetActiveScene();
        LevelContext2D context = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LevelContext2D>()).Single();
        foreach (EnemyHealth2D actor in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            actor.gameObject.SetActive(false);
        }
        context.Player.SetHealthCapacity(100, 100);
        var rows = new List<object>();
        int index = 0;
        foreach (string species in new[] { "ShadowStalker", "Sleeper" })
        {
            Vector3 origin = new Vector3(600, 600 + index * 15, 0);
            GameObject doorObject = context.CreateInstance(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/World/Door.prefab"), origin, Quaternion.identity);
            var door = doorObject.GetComponent<Door2D>();
            door.RestoreState(true);
            GameObject actor = context.CreateInstance(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/" + species + ".prefab"), origin + new Vector3(.2f, 0, 0), Quaternion.identity);
            var health = actor.GetComponent<EnemyHealth2D>();
            // Keep the actual living Rigidbody/collider, isolate closing physics from locomotion.
            foreach (Behaviour brain in actor.GetComponents<Behaviour>())
            {
                if (brain is ShadowStalker2D || brain is Sleeper2D || brain is CrowdAgent2D)
                {
                    brain.enabled = false;
                }
            }
            context.Player.transform.position = origin + new Vector3(-1.1f, 0, 0);
            Physics2D.SyncTransforms();
            int before = health.Current;
            Vector3 positionBefore = actor.transform.position;
            bool interacted = door.Interact(context.Player.GetComponent<PlayerInteraction>());
            await Task.Delay(900);
            rows.Add(new
            {
                species, interacted, before, after = health.Current,
                positionBefore = new[] { positionBefore.x, positionBefore.y },
                positionAfter = new[] { actor.transform.position.x, actor.transform.position.y },
                closed = !door.IsOpen && !door.IsMoving,
                colliderAlive = actor.GetComponent<Collider2D>().enabled,
                gore = actor.GetComponent<GoreBurst2D>().CaptureRemains() != null
            });
            UnityEngine.Object.Destroy(doorObject);
            UnityEngine.Object.Destroy(actor);
            index++;
        }
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "baseline.json", JsonConvert.SerializeObject(rows, Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        return rows;
    }

    public static async Task<object> PursuitDemo()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "DoorCrushTest")
        {
            throw new InvalidOperationException("Requires ordinary Play of the authored DoorCrushTest.");
        }
        Scene scene = SceneManager.GetActiveScene();
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>()).Single();
        context.Player.SetHealthCapacity(100, 100);
        context.Flashlight.SetPower(false);
        var rows = new List<object>();
        foreach (EnemyHealth2D health in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>()).OrderBy(actor => actor.transform.position.y))
        {
            float y = health.transform.position.y;
            Door2D door = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Door2D>())
                .Single(item => item.GetComponent<EmergencyShift.Features.Doors.View.DoorCrush2D>() != null && Mathf.Abs(item.transform.position.y - y) < 1);
            string species = health.GetComponent<Sleeper2D>() != null ? "sleeper" : "stalker";
            string folder = "work/door-crush01/frames-v2-" + species;
            Directory.CreateDirectory(folder);
            context.Player.transform.position = new Vector3(-1.1f, y, 0);
            context.Player.GetComponent<Rigidbody2D>().position = context.Player.transform.position;
            foreach (CameraFollow2D camera in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CameraFollow2D>()))
            {
                camera.SnapToTarget();
            }
            if (species == "sleeper")
            {
                // A real nonlethal hit wakes the sleeper; its own brain then pursues normally.
                health.GetComponent<Sleeper2D>().ReceiveBulletHit(1, health.transform.position, Vector2.left);
            }
            int deaths = 0;
            health.Died += () => deaths++;
            bool opened = door.Interact(context.Player.GetComponent<PlayerInteraction>());
            float deadline = Time.realtimeSinceStartup + 10;
            while (health.IsAlive && health.transform.position.x > .55f && Time.realtimeSinceStartup < deadline)
            {
                await Task.Delay(20);
            }
            Vector3 contactApproach = health.transform.position;
            if (!health.IsAlive || contactApproach.x > .55f)
            {
                throw new InvalidOperationException("The live pursuer did not reach the door: " + species + " at " + contactApproach);
            }
            ScreenCapture.CaptureScreenshot(folder + "/000.png");
            await Task.Delay(40);
            bool closed = door.Interact(context.Player.GetComponent<PlayerInteraction>());
            for (int frame = 1; frame <= 12; frame++)
            {
                ScreenCapture.CaptureScreenshot(folder + "/" + frame.ToString("D3") + ".png");
                await Task.Delay(40);
            }
            var gore = health.GetComponent<GoreBurst2D>();
            float settledDeadline = Time.realtimeSinceStartup + 5;
            while (!gore.IsSettled && Time.realtimeSinceStartup < settledDeadline)
            {
                await Task.Delay(50);
            }
            ScreenCapture.CaptureScreenshot(folder + "/settled.png");
            await Task.Delay(100);
            var stalkerAudio = health.GetComponent<StalkerAudio2D>();
            var sleeperAudio = health.GetComponent<SleeperAudio2D>();
            int sounds = stalkerAudio != null ? stalkerAudio.PlaybackCount(StalkerAudioSet.Cue.Death) : sleeperAudio.DeathCount;
            rows.Add(new
            {
                species, opened, closed,
                approach = new[] { contactApproach.x, contactApproach.y },
                health = health.Current, deaths, deathSounds = sounds,
                gore = gore.CaptureRemains() != null, settled = gore.IsSettled,
                doorFinished = !door.IsMoving && !door.IsOpen,
                playerHealth = context.Player.Health,
                method = "Real brain pursuit and Door.Interact, controlled player positioning; ordinary Play, no saved runtime edits"
            });
            if (health.IsAlive || deaths != 1 || sounds != 1 || door.IsMoving)
            {
                throw new InvalidOperationException("Pursuit/crush/audio failed: " + species);
            }
        }
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "pursuit-demo.json", JsonConvert.SerializeObject(rows, Formatting.Indented));
        return rows;
    }

    public static object InspectPlay()
    {
        Scene scene = SceneManager.GetActiveScene();
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>()).Single();
        var actor = context.Player.GetComponent<PlayerInteraction>();
        return new
        {
            Time.timeScale,
            player = context.Player.transform.position.ToString(),
            health = context.Player.Health,
            doors = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Door2D>()).Select(door => new
            {
                door.name, door.IsOpen, door.IsMoving,
                available = door.IsAvailable(actor),
                leaf = door.Blocker.bounds.ToString()
            }).ToArray(),
            enemies = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShadowStalker2D>()).Select(enemy => new
            {
                enemy.name, enemy.enabled, enemy.State, enemy.HasAggro,
                position = enemy.transform.position.ToString(),
                serialized = EditorJsonUtility.ToJson(enemy)
            }).ToArray()
        };
    }

    public static async Task<object> CaptureLayout()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "DoorCrushTest")
        {
            throw new InvalidOperationException("Requires ordinary Play of DoorCrushTest.");
        }
        Scene scene = SceneManager.GetActiveScene();
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>()).Single();
        float scale = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            context.Player.transform.position = new Vector3(-2.5f, 6, 0);
            foreach (CameraFollow2D camera in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CameraFollow2D>()))
            {
                camera.SnapToTarget();
            }
            string path = "work/door-crush01/final-layout.png";
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 5;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
            {
                await Task.Delay(50);
            }
            await Task.Delay(150);
            return new { path, captured = File.Exists(path), scope = "Final corridor boundary, doorway jambs and prefab signs in actual GameView" };
        }
        finally
        {
            Time.timeScale = scale;
        }
    }
}
