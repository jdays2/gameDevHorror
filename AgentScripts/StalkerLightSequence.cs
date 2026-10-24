using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Logic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StalkerLightSequence
{
    public static string Baseline() => Sample("baseline.json");
    public static string Verified() => Sample("model-timeline.json");

    public static string FinalState()
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var profiles = AssetDatabase.FindAssets("t:StalkerBehaviourProfile", new[] { "Assets/EmergencyShift" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(path => new
            {
                path,
                guid = AssetDatabase.AssetPathToGUID(path),
                dirty = EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<StalkerBehaviourProfile>(path))
            }).ToArray();
        var result = new
        {
            playing = EditorApplication.isPlaying,
            scenes = scenes.Select(scene => new { scene.path, scene.isDirty }).ToArray(),
            missingScripts = scenes.SelectMany(scene => scene.GetRootGameObjects())
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)),
            profiles,
            buildScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            buildOptions = "Ordinary Player; no Development or TestAssemblies flags supplied"
        };
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Docs/Validation/StalkerBurnSequence02/final-state.json", json);
        return json;
    }

    private static string Sample(string filename)
    {
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/EmergencyShift/Data/Enemies/Stalker-Default.asset");
        var profile = AssetDatabase.LoadAssetAtPath<StalkerBehaviourProfile>("Assets/EmergencyShift/Data/Enemies/Stalker-Light-Default.asset");
        var settings = profile.ToSettings(definition);
        var reports = new List<object>();
        foreach (float initialDamage in new[] { 0f, 1f })
        {
            var clock = new Clock();
            var model = new StalkerModel(settings, clock, Vector2.zero);
            if (initialDamage > 0)
            {
                new RestoreStalkerCommand(model, new StalkerStateSnapshot(StalkerState.Sneaking, true, initialDamage,
                    0, 0, 0, 0, Vector2.zero, Vector2.zero, 0, Vector2.zero)).Execute();
            }
            var events = new List<object>();
            var samples = new List<object>();
            model.EyeBurnStarted += () => events.Add(new { time = clock.Time, cue = "EyeBurn" });
            model.StateChanged += state => events.Add(new { time = clock.Time, state = state.ToString() });
            for (int index = 0; index < 160; index++)
            {
                clock.Time += .02;
                model.Step(new StalkerObservation(.02f, Vector2.zero, Vector2.zero, Vector2.right,
                    Vector2.right, Vector2.left, true, true, true, true, false, false, 3.2f, .3f, 256));
                samples.Add(new { time = clock.Time, state = model.State.ToString(), damage = model.EyeDamageProgress,
                    closure = model.Closure, pupilVisible = model.Closure < .166f });
            }

            reports.Add(new { initialDamage, events, samples });
        }

        const string directory = "Docs/Validation/StalkerBurnSequence02";
        Directory.CreateDirectory(directory);
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { settings, reports }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(directory, filename), json);
        return Path.Combine(directory, filename);
    }

    public static string Tune()
    {
        var results = new List<object>();
        foreach (string path in new[] { "Assets/EmergencyShift/Data/Enemies/Stalker-Light-Default.asset",
            "Assets/EmergencyShift/Data/Enemies/Stalker-Light-BasementPrototype.asset" })
        {
            var profile = AssetDatabase.LoadAssetAtPath<StalkerBehaviourProfile>(path);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("recoilSeconds").floatValue = 1.4f;
            serialized.FindProperty("eyeBurnSeconds").floatValue = 4;
            serialized.FindProperty("eyeOpenRecoilFraction").floatValue = .7f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            results.Add(new { path, guid = AssetDatabase.AssetPathToGUID(path), reaction = profile.ReactionDelay });
        }

        return Newtonsoft.Json.JsonConvert.SerializeObject(results);
    }

    public static async Task<string> Preview()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MorgueRouteTest")
        {
            throw new System.InvalidOperationException("Preview requires ordinary Play in MorgueRouteTest.");
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        LevelContext2D context = roots.SelectMany(root => root.GetComponentsInChildren<LevelContext2D>()).Single();
        foreach (ShadowStalker2D other in roots.SelectMany(root => root.GetComponentsInChildren<ShadowStalker2D>()))
        {
            other.gameObject.SetActive(false);
        }

        foreach (Canvas canvas in roots.SelectMany(root => root.GetComponentsInChildren<Canvas>()))
        {
            canvas.enabled = false;
        }

        context.Player.GetComponent<PlayerMotor2D>().enabled = false;
        context.Player.GetComponent<PlayerAim2D>().enabled = false;
        context.Player.GetComponent<PlayerWeapon2D>().enabled = false;
        context.Player.GetComponent<Rigidbody2D>().position = new Vector2(101.5f, 100);
        context.Player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        context.Player.transform.position = new Vector3(101.5f, 100, 0);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        var actor = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity).GetComponent<ShadowStalker2D>();
        actor.AttacksEnabled = false;
        actor.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        Camera camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
        camera.GetComponent<CameraFollow2D>().enabled = false;
        camera.transform.position = new Vector3(100, 100, -10);
        camera.orthographicSize = .8f;
        Physics2D.SyncTransforms();
        context.Flashlight.Acquire();
        context.Flashlight.SetPower(false);
        await Task.Delay(250);
        const string directory = "Docs/Validation/StalkerBurnSequence02";
        var samples = new List<object>();
        foreach (string stage in new[] { "healthy", "contact", "early-burn", "stopped-burn", "closed-before-run" })
        {
            if (stage == "contact")
            {
                context.Flashlight.AimAt(actor.transform.position);
                context.Flashlight.SetPower(true);
                await Task.Delay(70);
            }
            else if (stage == "early-burn")
            {
                await Task.Delay(280);
            }
            else if (stage == "stopped-burn")
            {
                await Task.Delay(650);
            }
            else if (stage == "closed-before-run")
            {
                await Task.Delay(650);
            }

            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(directory, stage + ".png")));
            var audio = actor.GetComponent<StalkerAudio2D>();
            samples.Add(new { stage, time = Time.time, frame = Time.frameCount, state = actor.State.ToString(),
                damage = actor.EyeDamageProgress, closure = actor.Closure,
                burnCues = audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), clip = audio.MovementSource.clip?.name });
        }

        await Task.Delay(250);
        File.WriteAllText(Path.Combine(directory, "preview.json"), Newtonsoft.Json.JsonConvert.SerializeObject(samples, Newtonsoft.Json.Formatting.Indented));
        return Path.Combine(directory, "preview.json");
    }

    private sealed class Clock : IGameClock
    {
        public double Time { get; set; }
    }
}
