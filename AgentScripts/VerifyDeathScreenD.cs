using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using EmergencyShift.Features.WorldAudio.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using EmergencyShift.Features.WorldAudio.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

public static class VerifyDeathScreenD
{
    private const string Evidence = "Docs/Validation/DeathScreenD01/";

    public static object Stopped()
    {
        Scene scene = SceneManager.GetActiveScene();
        WorldAudioProfile audio = AssetDatabase.LoadAssetAtPath<WorldAudioProfile>(
            "Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset");
        bool hasGain = audio.WorldOutput.audioMixer.GetFloat(audio.AttenuationParameter, out float gainDb);
        var result = new
        {
            scene = scene.path,
            sceneCount = SceneManager.sceneCount,
            scene.isDirty,
            playing = Application.isPlaying,
            startScene = EditorSceneManager.playModeStartScene != null
                ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : null,
            timeScale = Time.timeScale,
            listenerPause = AudioListener.pause,
            listenerVolume = AudioListener.volume,
            mixerGainDb = gainDb,
            savedDeathSources = One<DeathScreenEnvironment>(scene).GetComponentsInChildren<AudioSource>(true).Length,
            fixtureExists = AssetDatabase.IsValidFolder("Assets/EmergencyShift/Tests/Fixtures/DeathScreenTask006")
        };
        if (Application.isPlaying || scene.isDirty || SceneManager.sceneCount != 1 ||
            EditorSceneManager.playModeStartScene != null || Time.timeScale != 1 || AudioListener.pause ||
            AudioListener.volume != 1 || !hasGain || Mathf.Abs(gainDb) > .01f || result.fixtureExists ||
            result.savedDeathSources != 0)
        {
            throw new InvalidOperationException("Final Editor state is not restored: " + JsonConvert.SerializeObject(result));
        }
        Save("final-state.json", result);
        return result;
    }

    public sealed class Services
    {
        [Inject] public GameplayInputBlock Input { get; private set; }
        [Inject] public WorldMotionPause Motion { get; private set; }
        [Inject] public WorldAudioBus Audio { get; private set; }
    }

    public static object State()
    {
        Scene scene = SceneManager.GetActiveScene();
        LevelContext2D context = One<LevelContext2D>(scene);
        DeathScreenPresenter presenter = One<DeathScreenPresenter>(scene);
        var services = new Services();
        context.InjectScoped(services);
        var result = new
        {
            scene = scene.path,
            playerId = context.Player.GetEntityId().ToString(),
            health = context.Player.Health,
            phase = presenter.Controller.Model.Phase.ToString(),
            presenter.Controller.Model.Elapsed,
            inputBlocked = services.Input.IsBlocked,
            motionPaused = services.Motion.IsActive,
            worldGain = services.Audio.Gain,
            listenerPause = AudioListener.pause,
            listenerVolume = AudioListener.volume,
            timeScale = Time.timeScale,
            cursorVisible = Cursor.visible,
            cursorLock = Cursor.lockState.ToString(),
            hudCanvases = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameplayHudView>(true))
                .Select(hud => new { hud.name, enabled = hud.GetComponentInParent<Canvas>(true).enabled }).ToArray(),
            deathSources = presenter.GetComponentsInChildren<AudioSource>(true).Length,
            visible = One<DeathScreenView>(scene).IsVisible,
            activeEventSystems = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                .Count(events => events.isActiveAndEnabled),
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                ? EventSystem.current.currentSelectedGameObject.name : null,
            screen = new { Screen.width, Screen.height }
        };
        return result;
    }

    public static async Task<object> Death()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Ordinary Play is required.");
        }
        await Task.Delay(150);
        object before = State();
        LevelContext2D context = One<LevelContext2D>(SceneManager.GetActiveScene());
        if (!context.Player.TakeDamage(context.Player.MaximumHealth + 1000))
        {
            throw new InvalidOperationException("Lethal damage was rejected.");
        }
        await Task.Delay(500);
        object entering = State();
        await Task.Delay(1200);
        object ready = State();
        Directory.CreateDirectory(Evidence);
        string path = Evidence + "initial-death.png";
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(200);
        var result = new { before, entering, ready, screenshot = path };
        Save("native-initial.json", result);
        return result;
    }

    public static async Task<object> Retry()
    {
        Scene scene = SceneManager.GetActiveScene();
        DeathScreenPresenter old = One<DeathScreenPresenter>(scene);
        object before = State();
        if (!old.Controller.Execute(DeathScreenIntent.Retry))
        {
            throw new InvalidOperationException("Retry was rejected.");
        }
        bool duplicateRejected = !old.Controller.Execute(DeathScreenIntent.Retry);
        await Task.Delay(2200);
        object after = State();
        var result = new { before, after, duplicateRejected, oldDestroyed = old == null };
        Save("native-first-retry.json", result);
        return result;
    }

    private static T One<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static void Save(string file, object data)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + file, JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}
