using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Pickups.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PickupGlintReview
{
    private const string Output = "Docs/Validation/PickupGlint57/";
    private const string StartSceneFile = "work/PickupGlint57/original-play-start.txt";

    public static object SelectCaptureScene()
    {
        string previous = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
        File.WriteAllText(StartSceneFile, previous);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity");
        return new { previous, sceneSaved = false };
    }

    public static object RestoreCaptureScene()
    {
        string previous = File.ReadAllText(StartSceneFile);
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        return new { restored = previous, sceneSaved = false };
    }

    public static async Task<object> Capture()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Capture requires a disposable Play session.");
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        var context = roots.SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        foreach (GameObject root in roots)
        {
            foreach (EnemyHealth2D enemy in root.GetComponentsInChildren<EnemyHealth2D>(true))
            {
                enemy.gameObject.SetActive(false);
            }
            foreach (MonoBehaviour item in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (item is IPickupGlintTarget)
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        var player = context.Player;
        player.GetComponent<PlayerMotor2D>().enabled = false;
        player.GetComponent<PlayerAim2D>().enabled = false;
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        var weapon = player.GetComponent<PlayerWeapon2D>();
        var shotgun = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PumpShotgun.asset");
        weapon.RestoreState(true, 1, shotgun);
        var actor = player.GetComponent<PlayerInteraction>();
        var light = context.Flashlight;
        light.Acquire();
        light.SetPower(true);
        Vector2 origin = player.transform.position;
        Vector2 direction = Vector2.up;
        bool found = false;
        foreach (Vector2 candidate in new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left })
        {
            light.AimAt(origin + candidate * 3);
            Vector2 tangent = new Vector2(candidate.y, -candidate.x);
            if (light.Illuminates(origin + candidate * 1.8f, actor.BlockingGeometry) &&
                light.Illuminates(origin + candidate * 2.4f + tangent * .45f, actor.BlockingGeometry) &&
                light.Illuminates(origin + candidate * 2.8f - tangent * .45f, actor.BlockingGeometry))
            {
                direction = candidate;
                found = true;
                break;
            }
        }
        if (!found)
        {
            throw new InvalidOperationException("No unobstructed presentation space at the authored start.");
        }

        Vector2 side = new Vector2(direction.y, -direction.x);
        var created = new List<GameObject>();
        float oldTimeScale = Time.timeScale;
        try
        {
            created.Add(Spawn(context, "Loot/PistolPickup", origin + direction * 1.8f));
            created.Add(Spawn(context, "Loot/ShotgunAmmoPickup", origin + direction * 2.4f + side * .45f));
            created.Add(Spawn(context, "World/AccessCards01/ResearchCard", origin + direction * 2.8f - side * .45f));
            var glints = created.Select(item => item.GetComponent<PickupGlint2D>()).ToArray();
            light.AimAt(origin + direction * 3);
            light.WorldCamera.GetComponent<CameraFollow2D>().SnapToTarget();
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Repaint();
            double deadline = EditorApplication.timeSinceStartup + 3;
            while ((!glints.All(item => item.IsShowing) || glints[0].Progress < .45f) && EditorApplication.timeSinceStartup < deadline)
            {
                await Task.Delay(8);
            }
            if (!glints.All(item => item.IsShowing))
            {
                throw new InvalidOperationException("All three eligible items must be showing their real glint.");
            }

            Time.timeScale = 0;
            await Task.Delay(70);
            Save("game-glint-peak.png");
            float capturedProgress = glints[0].Progress;
            Time.timeScale = oldTimeScale;
            await Task.Delay(750);
            Save("game-after-glint.png");
            bool ended = glints.All(item => !item.IsShowing);
            light.SetPower(false);
            await Task.Delay(100);
            Save("game-flashlight-off.png");
            var result = new { scene = context.gameObject.scene.path, items = created.Select(item => item.name).ToArray(),
                capturedProgress, ended, flashlightOnly = glints.All(item => !item.IsShowing), cameraSize = light.WorldCamera.orthographicSize,
                controlledReview = "Temporary eligible pickups, owned shotgun with one shell, enemies disabled; authored positions and scenes are never saved.",
                sceneSaved = false };
            File.WriteAllText(Output + "game-capture.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            Time.timeScale = oldTimeScale;
            foreach (GameObject item in created)
            {
                Object.Destroy(item);
            }
        }
    }

    public static object Build()
    {
        const string destination = "Builds/PickupGlintValidation/EmergencyShift.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = destination,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        string[] ownAssemblies = Directory.GetFiles("Builds/PickupGlintValidation/EmergencyShift_Data/Managed", "EmergencyShift*.dll")
            .Select(Path.GetFileName).ToArray();
        var result = new { result = report.summary.result.ToString(), errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings, bytes = report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds,
            ownAssemblies, profileIncluded = report.packedAssets.SelectMany(pack => pack.contents).Any(item => item.sourceAssetPath == "Assets/EmergencyShift/Resources/PickupGlintProfile.asset") };
        File.WriteAllText(Output + "build.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        if (report.summary.result != BuildResult.Succeeded || ownAssemblies.Any(name => name.Contains("Tests") || name.Contains("Development") || name.Contains("Editor")))
        {
            throw new InvalidOperationException("Pickup glint release validation failed.");
        }
        return result;
    }

    public static object BuildMessages()
    {
        var messages = BuildReport.GetLatestReport().steps.SelectMany(step => step.messages)
            .Where(message => message.type == LogType.Warning || message.type == LogType.Error)
            .Select(message => new { type = message.type.ToString(), message.content }).ToArray();
        File.WriteAllText(Output + "build-messages.json", JsonConvert.SerializeObject(messages, Formatting.Indented));
        return messages;
    }

    private static GameObject Spawn(LevelContext2D context, string relative, Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/" + relative + ".prefab");
        return context.CreateInstance(prefab, position, Quaternion.identity);
    }

    private static void Save(string name)
    {
        Canvas.ForceUpdateCanvases();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        if (image == null || image.width < 100)
        {
            throw new InvalidOperationException("Game view capture unavailable.");
        }
        File.WriteAllBytes(Output + name, image.EncodeToPNG());
        Object.Destroy(image);
    }
}
