using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Enemies.View;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ReviewStalkerRig
{
    public static async Task<string> Live()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires Play; stop afterwards to restore authored scenes.");
        }
        string folder = "Concepts/Used/Stalker/Locomotion02";
        var reports = new List<object>();
        foreach (string level in new[] { "MechanicsSandbox", "MorgueRouteTest" })
        {
            string path = "Assets/EmergencyShift/Scenes/" + level + ".unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                while (!load.isDone) await Task.Yield();
                scene = SceneManager.GetSceneByPath(path);
            }
            SceneManager.SetActiveScene(scene);
            var actors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShadowStalker2D>(true)).ToArray();
            LevelContext2D context = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelContext2D>(true)).First();
            Camera camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First(c => c.orthographic);
            ShadowStalker2D focus = actors.First(a => a.gameObject.activeInHierarchy && a.GetComponent<EmergencyShift.Features.ScriptedEncounters.View.ScriptedStalkerRoute2D>() == null);
            foreach (ShadowStalker2D actor in actors)
            {
                if (actor != focus)
                {
                    actor.gameObject.SetActive(false);
                }
            }
            foreach (Sleeper2D sleeper in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Sleeper2D>(true)))
            {
                sleeper.gameObject.SetActive(false);
            }
            context.Player.SetHealthCapacity(100, 100);
            context.Player.GetComponent<PlayerMotor2D>().enabled = false;
            Vector2 playerAt = ClearPlayerPoint(focus);
            Rigidbody2D playerBody = context.Player.GetComponent<Rigidbody2D>();
            playerBody.position = playerAt;
            playerBody.linearVelocity = Vector2.zero;
            context.Player.transform.position = playerAt;
            bool grantedFlashlight = !context.Flashlight.IsOwned;
            if (grantedFlashlight)
            {
                context.Flashlight.RestoreOwnership(true);
            }
            context.Flashlight.SetPower(true);
            context.Flashlight.AimAt(focus.transform.position);
            focus.AttacksEnabled = false;
            for (int shot = 0; shot < 5; shot++)
            {
                double until = EditorApplication.timeSinceStartup + .3;
                while (EditorApplication.timeSinceStartup < until)
                {
                    context.Flashlight.AimAt(focus.transform.position);
                    await Task.Yield();
                }
                camera.transform.position = focus.transform.position + new Vector3(0, -1.1f, -10);
                Capture(camera, folder + "/" + level + "-live-" + shot + ".png");
                reports.Add(new { level, shot, grantedFlashlight, camera.orthographicSize, actor = focus.name, state = focus.State.ToString(),
                    velocity = focus.Velocity.ToString("R"), position = focus.transform.position.ToString("R"),
                    rigCorrections = focus.GetComponent<StalkerLimbRig2D>().Gait.ReachCorrections });
            }
        }
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(reports, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(folder + "/live-review.json", json);
        return json;
    }

    private static Vector2 ClearPlayerPoint(ShadowStalker2D focus)
    {
        EnemyMotor2D motor = focus.GetComponent<EnemyMotor2D>();
        Vector2 origin = focus.transform.position;
        foreach (float radius in new[] { 2.7f, 2.3f, 2f })
        {
            for (int direction = 0; direction < 8; direction++)
            {
                Vector2 candidate = origin + (Vector2)(Quaternion.Euler(0, 0, direction * 45) * Vector2.down) * radius;
                if (motor.CanStand(candidate) && motor.Clear(candidate, origin))
                {
                    return candidate;
                }
            }
        }
        throw new InvalidOperationException("No clear nearby camera/flashlight test position for " + focus.name);
    }

    private static void Capture(Camera camera, string path)
    {
        var target = new RenderTexture(1280, 720, 24);
        target.Create();
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.Destroy(pixels);
            target.Release();
            UnityEngine.Object.Destroy(target);
        }
    }

    public static object Build()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play first.");
        }
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity", "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity" },
            locationPathName = "Builds/StalkerLocomotion02/EmergencyShift.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CompressWithLz4HC
        });
        var result = new { result = report.summary.result.ToString(), errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
            messages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content).ToArray() };
        File.WriteAllText("Concepts/Used/Stalker/Locomotion02/build-result.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Stalker verification build failed.");
        }
        return result;
    }

    public static object Verify()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Verification needs stopped Editor.");
        }
        var baseline = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Concepts/Used/Stalker/Locomotion02/baseline-placements.json"));
        var scenes = new List<object>();
        foreach (var row in baseline["rows"])
        {
            string path = (string)row["path"];
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
            var actors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShadowStalker2D>(true)).ToArray();
            if (actors.Length != row["actors"].Count())
            {
                throw new InvalidOperationException("Actor count changed in " + path);
            }
            foreach (var before in row["actors"])
            {
                ShadowStalker2D actor = actors.Single(a => Hierarchy(a.transform) == (string)before["path"]);
                if (actor.transform.position.ToString("R") != (string)before["position"] || actor.transform.lossyScale.ToString("R") != (string)before["scale"])
                {
                    throw new InvalidOperationException("Placement changed: " + actor.name);
                }
                var oldFields = Newtonsoft.Json.Linq.JObject.Parse((string)before["serialized"])["MonoBehaviour"];
                var newFields = Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(actor))["MonoBehaviour"];
                ((Newtonsoft.Json.Linq.JObject)oldFields).Remove("m_EditorClassIdentifier");
                ((Newtonsoft.Json.Linq.JObject)newFields).Remove("m_EditorClassIdentifier");
                if (!Newtonsoft.Json.Linq.JToken.DeepEquals(oldFields, newFields))
                {
                    throw new InvalidOperationException("Gameplay serialization changed: " + actor.name);
                }
                var rig = actor.GetComponent<StalkerLimbRig2D>();
                var properties = new SerializedObject(rig);
                foreach (string field in new[] { "profile", "visual", "body", "iris", "pupil", "backing", "contact", "projection" })
                {
                    if (properties.FindProperty(field).objectReferenceValue == null)
                    {
                        throw new InvalidOperationException("Missing rig reference " + actor.name + ": " + field);
                    }
                }
            }
            scenes.Add(new { path, count = actors.Length, scene.isDirty, placementAndGameplayFieldsUnchanged = true });
            if (opened)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        var shaders = new List<object>();
        foreach (string name in new[] { "StalkerRigSurface", "StalkerRigShadow" })
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/EmergencyShift/Materials/Characters/" + name + ".shader");
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
            if (errors.Length != 0)
            {
                throw new InvalidOperationException(string.Join("; ", errors));
            }
            shaders.Add(new { name, shader.isSupported, errors });
        }
        var result = new { scenes, shaders, playModeStartScene = EditorSceneManager.playModeStartScene,
            authoringGate = "Not applicable: no scene objects added/moved and no scene saved", releaseBuildVerified = false,
            releaseLimitation = "Insufficient free disk space on C; see build-result.json" };
        File.WriteAllText("Concepts/Used/Stalker/Locomotion02/preservation-and-shaders.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    private static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;
}
