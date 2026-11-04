using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DeathScreen.Config;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Config-only instant entry and a disposable ordinary-Play check. Never saves scenes.</summary>
public static class AdjustDeathScreenD03
{
    private const string Evidence = "Docs/Validation/DeathScreenD03/";
    private const string ProfilePath = "Assets/EmergencyShift/Data/UI/DeathScreen/Transition.asset";

    public static object Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling, "Use a stopped compiled Editor under the game lease.");
        DeathScreenProfile profile = AssetDatabase.LoadAssetAtPath<DeathScreenProfile>(ProfilePath);
        DeathScreenSettings before = profile.ToSettings();
        var serialized = new SerializedObject(profile);
        serialized.FindProperty("worldFadeSeconds").floatValue = 0;
        serialized.FindProperty("contentDelaySeconds").floatValue = 0;
        serialized.FindProperty("contentFadeSeconds").floatValue = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(profile);
        DeathScreenSettings after = profile.ToSettings();
        Require(after.EntrySeconds == 0 && after.WorldPauseDelaySeconds == before.WorldPauseDelaySeconds &&
            after.ExitFadeSeconds == before.ExitFadeSeconds && after.IrisFeather == before.IrisFeather, "Only entry durations may change.");
        var result = new { profile = ProfilePath, before, after, authoredScenesSaved = 0, runtimeCodeChanged = false };
        Save("profile.json", result);
        return result;
    }

    public static object Start()
    {
        Require(Application.isPlaying, "Ordinary Play is required.");
        Save("native-status.json", new { status = "running" });
        One<DeathScreenEnvironment>().StartCoroutine(Guard(Capture()));
        return new { status = "scheduled" };
    }

    private static IEnumerator Guard(IEnumerator routine)
    {
        try
        {
            while (true)
            {
                bool next;
                try
                {
                    next = routine.MoveNext();
                }
                catch (Exception error)
                {
                    Save("native-status.json", new { status = "failed", error = error.ToString() });
                    yield break;
                }
                if (!next)
                {
                    yield break;
                }
                yield return routine.Current;
            }
        }
        finally
        {
            (routine as IDisposable)?.Dispose();
        }
    }

    private static IEnumerator Capture()
    {
        yield return new WaitForEndOfFrame();
        DeathScreenPresenter presenter = One<DeathScreenPresenter>();
        DeathScreenModel model = presenter.Controller.Model;
        float started = Time.realtimeSinceStartup;
        Require(model.Settings.EntrySeconds == 0, "The actual profile must have instant entry.");
        Require(One<LevelContext2D>().Player.TakeDamage(100000), "The actual player must die.");
        DeathScreenView view = One<DeathScreenView>();
        CanvasGroup content = Field<CanvasGroup>(view, "content");
        CanvasGroup shade = Field<CanvasGroup>(view, "shade");
        Require(model.IsVisible && model.ClosureProgress == 1 && model.ContentOpacity == 1 &&
            view.IsVisible && content.alpha == 1 && shade.alpha == 1, "Death must synchronously show the complete menu on black.");
        var immediate = new { seconds = Time.realtimeSinceStartup - started, phase = model.Phase.ToString(),
            model.ClosureProgress, model.ContentOpacity, view.IsVisible, actualContentAlpha = content.alpha,
            actualShadeAlpha = shade.alpha, timeScale = Time.timeScale };
        yield return new WaitForEndOfFrame();
        Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
        int blackSamples = 0;
        try
        {
            for (int y = 0; y < frame.height; y += 17)
            {
                for (int x = frame.width * 3 / 4; x < frame.width; x += 17)
                {
                    Color32 pixel = frame.GetPixel(x, y);
                    Require(pixel.r == 0 && pixel.g == 0 && pixel.b == 0, "The first rendered death frame must not leak the world.");
                    blackSamples++;
                }
            }
            File.WriteAllBytes(Evidence + "first-frame.png", frame.EncodeToPNG());
        }
        finally
        {
            Object.Destroy(frame);
        }
        Require(model.ContentOpacity == 1 && model.ClosureProgress == 1, "The menu must stay fully visible.");
        while (Time.timeScale != 0)
        {
            Require(Time.realtimeSinceStartup - started < 3, "Existing world pause did not complete.");
            yield return null;
        }
        var result = new { immediate, firstRenderedFrameBlackSamples = blackSamples,
            readyPhase = model.Phase.ToString(), worldPauseSeconds = Time.realtimeSinceStartup - started,
            configuredWorldPauseSeconds = model.Settings.WorldPauseDelaySeconds,
            deathAudioSources = view.transform.root.GetComponentsInChildren<AudioSource>(true).Length };
        Require(model.IsInteractive && result.deathAudioSources == 0, "The completed menu must accept actions without music.");
        Save("native.json", result);
        Save("native-status.json", new { status = "completed" });
    }

    public static async Task<object> Retry()
    {
        DeathScreenPresenter old = One<DeathScreenPresenter>();
        Require(old.Controller.Execute(DeathScreenIntent.Retry), "Retry rejected.");
        bool duplicateRejected = !old.Controller.Execute(DeathScreenIntent.Retry);
        await Task.Delay(1800);
        var result = new { oldDestroyed = old == null, alive = !One<LevelContext2D>().Player.IsDead,
            phase = One<DeathScreenPresenter>().Controller.Model.Phase.ToString(), timeScale = Time.timeScale, duplicateRejected };
        Require(result.oldDestroyed && result.alive && result.phase == "Hidden" && result.timeScale == 1 && duplicateRejected, "Retry must restore a new living scope once.");
        Save("retry.json", result);
        return result;
    }

    public static object State()
    {
        Scene scene = SceneManager.GetActiveScene();
        var result = new { scene = scene.path, scene.isDirty, sceneCount = SceneManager.sceneCount,
            playing = Application.isPlaying, startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            timeScale = Time.timeScale, listenerPause = AudioListener.pause, listenerVolume = AudioListener.volume };
        Require(!scene.isDirty && SceneManager.sceneCount == 1 && !Application.isPlaying &&
            EditorSceneManager.playModeStartScene == null && Time.timeScale == 1 && !AudioListener.pause && AudioListener.volume == 1, "Editor must return to its clean stopped map.");
        Save("final-state.json", result);
        return result;
    }

    private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Exclude).Single();
    private static T Field<T>(object source, string name) => (T)source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
    private static void Save(string name, object value)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + name, JsonConvert.SerializeObject(value, Formatting.Indented));
    }
}
