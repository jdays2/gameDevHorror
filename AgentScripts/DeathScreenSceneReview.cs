using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeathScreenSceneReview
{
    private const string Evidence = "Docs/Validation/DeathScreenD01/";

    public static object OpenMorgue() => Open("MorgueRouteTest");
    public static object OpenBasement() => Open("BasementPrototype");
    public static object OpenTemplate() => Open("LevelTemplate");
    public static object OpenDoor() => Open("DoorCrushTest");
    public static object OpenMechanics() => Open("MechanicsSandbox");

    private static object Open(string map)
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("One clean stopped scene is required.");
        }
        Scene scene = EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/" + map + ".unity", OpenSceneMode.Single);
        return new { scene = scene.path, scene.isDirty };
    }

    public static async Task<object> RunCurrent()
    {
        Require(Application.isPlaying, "Ordinary Play is required.");
        string path = SceneManager.GetActiveScene().path;
        string map = Path.GetFileNameWithoutExtension(path);
        await Task.Delay(500);
        LevelContext2D context = One<LevelContext2D>();
        string oldId = context.Player.GetEntityId().ToString();
        context.Player.TakeDamage(context.Player.MaximumHealth + 1000);
        await Until(() => Presenter().Controller.Model.Phase == DeathScreenPhase.Ready, "Death: " + map);
        Require(Time.timeScale == 0 && Presenter().GetComponentsInChildren<AudioSource>(true).Length == 0,
            "Frozen and silent D: " + map);
        if (map == "MorgueRouteTest")
        {
            ScreenCapture.CaptureScreenshot(Evidence + "morgue-runtime.png");
            await Task.Delay(200);
        }
        DeathScreenPresenter old = Presenter();
        Require(old.Controller.Execute(DeathScreenIntent.Retry), "Retry: " + map);
        await Until(() => old == null && Presenter() != null && Presenter().Controller != null &&
            Presenter().Controller.Model.Phase == DeathScreenPhase.Hidden, "Single reload: " + map);
        await Task.Delay(200);
        Require(!One<LevelContext2D>().Player.IsDead && Time.timeScale == 1 &&
            One<LevelContext2D>().Player.GetEntityId().ToString() != oldId, "Fresh scope: " + map);
        var result = new { scene = path, deathReady = true, singleReload = true, newPlayer = true,
            noMusicSources = true, timeRestored = true };
        Save("scene-" + map + ".json", result);
        return result;
    }

    private static DeathScreenPresenter Presenter() => All<DeathScreenPresenter>().SingleOrDefault();
    private static T One<T>() where T : Component => All<T>().Single();
    private static IEnumerable<T> All<T>() where T : Component => SceneManager.GetActiveScene()
        .GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));

    private static async Task Until(Func<bool> test, string label)
    {
        DateTime limit = DateTime.UtcNow.AddSeconds(15);
        while (!test())
        {
            Require(DateTime.UtcNow < limit, "Timed out: " + label);
            await Task.Delay(50);
        }
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
    }

    private static void Save(string file, object data)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + file, JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}
