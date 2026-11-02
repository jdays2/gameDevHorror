using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using EmergencyShift.Features.PauseMenu.Logic;
using EmergencyShift.Features.PauseMenu.View;
using EmergencyShift.Features.WorldAudio.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

public static class ExerciseDeathScreenD
{
    private const string Evidence = "Docs/Validation/DeathScreenD01/";
    private static readonly List<object> results = new List<object>();

    public sealed class Services
    {
        [Inject] public GameplayInputBlock Input { get; private set; }
        [Inject] public WorldMotionPause Motion { get; private set; }
        [Inject] public PauseMenuController Pause { get; private set; }
        [Inject] public WorldAudioBus Audio { get; private set; }
    }

    public static object Start()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Ordinary Play from MechanicsSandbox is required.");
        }
        Directory.CreateDirectory(Evidence);
        Save("interaction-status.json", new { status = "running" });
        EditorApplication.delayCall += async () =>
        {
            try
            {
                await Run();
                Save("interaction-status.json", new { status = "completed", cases = results.Count });
            }
            catch (Exception error)
            {
                Save("interaction-status.json", new { status = "failed", error = error.ToString(), results });
            }
        };
        return new { status = "scheduled", statusFile = Evidence + "interaction-status.json" };
    }

    private static async Task Run()
    {
        results.Clear();
        InputSettings originalSettings = InputSystem.settings;
        InputSettings settings = Object.Instantiate(originalSettings);
        float originalVolume = AudioListener.volume;
        Keyboard keyboard = null;
        IDisposable externalInput = null;
        IDisposable externalMotion = null;
        try
        {
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = settings;
            keyboard = InputSystem.AddDevice<Keyboard>("TASK006 virtual keyboard");
            AudioListener.volume = .37f;
            await Task.Delay(150);
            LevelContext2D context = Context();
            context.Player.SetHealthCapacity(100, 100);
            Services service = Resolve(context);
            PauseMenuMusic music = All<PauseMenuMusic>().Single();

            Require(service.Pause.TryOpen(), "Pause must open while alive.");
            await Task.Delay(200);
            Require(AudioListener.pause && service.Pause.IsOpen && music.IsPlaying, "Real pause and music did not open.");
            Kill();
            Require(!service.Pause.IsOpen && !music.IsPlaying && !music.gameObject.activeSelf &&
                !AudioListener.pause, "Death must close pause leases and both music voices.");
            await Ready();
            Record("death-from-open-pause", new { musicStopped = !music.IsPlaying, listenerPreserved = !AudioListener.pause });

            externalInput = service.Input.Acquire();
            externalMotion = service.Motion.Acquire();
            context.Player.RestoreHealth(100);
            await Hidden();
            Require(service.Input.IsBlocked && service.Motion.IsActive && Time.timeScale == 0,
                "Returning UI must not release external leases.");
            Require(music.gameObject.activeSelf && !music.IsPlaying, "Pause root must return closed and silent.");
            Record("external-owners-preserved", new { input = service.Input.IsBlocked, motion = service.Motion.IsActive });
            externalInput.Dispose();
            externalInput = null;
            externalMotion.Dispose();
            externalMotion = null;
            await Task.Delay(70);
            Require(!service.Input.IsBlocked && Time.timeScale == 1, "Only external owners should remain.");

            Require(service.Pause.TryOpen(), "Pause must reopen after restore.");
            await Task.Delay(150);
            Require(service.Pause.TryResume(), "Pause must start closing.");
            await Task.Delay(50);
            Require(music.IsPlaying, "Closing pause must have a real pending fade for this case.");
            Kill();
            await Ready();
            Require(!music.IsPlaying, "Closing pause music leaked into death.");
            Record("death-from-closing-pause", new { musicStopped = !music.IsPlaying });
            context.Player.RestoreHealth(100);
            await Hidden();

            Require(service.Pause.TryOpen(), "Pause must open for pending quit.");
            await Task.Delay(100);
            var pauseQuit = music.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button => button.name == "Quit");
            pauseQuit.onClick.Invoke();
            Kill();
            await Ready();
            await Task.Delay(250);
            Require(!service.Pause.IsOpen && !music.IsPlaying && !music.gameObject.activeSelf,
                "Pending pause quit must remain cancelled during death.");
            context.Player.RestoreHealth(100);
            await Hidden();
            await Task.Delay(900);
            Require(!service.Pause.IsOpen && !music.IsPlaying && !service.Input.IsBlocked,
                "A cancelled pause quit must not resume or reopen after restore.");
            Record("pending-pause-quit-cancelled", new { restoredClosed = !service.Pause.IsOpen });

            DialogueSession dialogue = All<DialogueSession>().Single();
            string sequencePath = AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:DialogueSequence", new[] { "Assets/EmergencyShift" }).First());
            DialogueSequence sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(sequencePath);
            Require(dialogue.TryBegin(sequence), "Real dialogue must open.");
            Kill();
            await Ready();
            Require(!dialogue.IsOpen, "Death must cancel dialogue.");
            Record("death-from-dialogue", new { dialogueClosed = !dialogue.IsOpen });

            DeathScreenButton[] buttons = Presenter().GetComponentsInChildren<DeathScreenButton>(true);
            DeathScreenButton primary = buttons.Single(button => button.name == "Retry or Stay");
            DeathScreenButton secondary = buttons.Single(button => button.name == "Quit or Confirm");
            Click(secondary);
            Require(Phase() == DeathScreenPhase.ConfirmQuit &&
                EventSystem.current.currentSelectedGameObject == primary.gameObject,
                "Quit confirmation must default to Stay.");
            await Press(keyboard, Key.Enter);
            Require(Phase() == DeathScreenPhase.Ready, "Default confirmation must stay.");
            Click(secondary);
            await Press(keyboard, Key.Escape);
            Require(Phase() == DeathScreenPhase.Ready && context.Player.IsDead,
                "Esc must cancel only confirmation, never resume a dead player.");
            Click(secondary);
            Click(secondary);
            await WaitUntil(() => Phase() == DeathScreenPhase.Error, "Editor exit feedback");
            Require(context.Player.IsDead && service.Input.IsBlocked && Time.timeScale == 0,
                "Editor exit rejection must retain the death state.");
            Record("pointer-keyboard-exit-confirmation", new { defaultStay = true, escapeStayed = true, editorExitStayedDead = true });

            string oldPlayer = context.Player.GetEntityId().ToString();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            await Task.Delay(130);
            Require(Phase() == DeathScreenPhase.WaitingRetry &&
                Context().Player.GetEntityId().ToString() == oldPlayer, "Held R must not bypass UI via PlayerLife.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(120);
            Require(Phase() == DeathScreenPhase.LeavingRetry, "R release must start the outgoing fade.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            await WaitUntil(() => Context().Player.GetEntityId().ToString() != oldPlayer, "Real R reload");
            context = Context();
            context.Player.SetHealthCapacity(100, 100);
            service = Resolve(context);
            string newPlayer = context.Player.GetEntityId().ToString();
            await Task.Delay(200);
            Require(Phase() == DeathScreenPhase.Hidden && service.Input.IsBlocked, "New scene must fence a held R.");
            await Task.Delay(150);
            Require(Context().Player.GetEntityId().ToString() == newPlayer, "Held R must not restart the fresh scene twice.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await WaitUntil(() => !Resolve(Context()).Input.IsBlocked, "New scene input release");
            Require(Time.timeScale == 1 && service.Audio.Gain == 1 &&
                Mathf.Abs(AudioListener.volume - .37f) < .0001f, "Reload must restore world/user audio.");
            Record("real-R-reload-held-across-load", new { newPlayer, duplicateReload = false, inputReleased = !service.Input.IsBlocked });

            Kill();
            await Ready();
            Require(All<EventSystem>().Count(item => item.isActiveAndEnabled) == 1, "Exactly one EventSystem must be active.");
            Require(Presenter().GetComponentsInChildren<AudioSource>(true).Length == 0, "Death screen must contain no music sources.");
            Record("repeat-death-after-real-reload", new { worldSilent = service.Audio.Gain == 0, noMusic = true });
            Save("native-interactions.json", results);
        }
        finally
        {
            externalInput?.Dispose();
            externalMotion?.Dispose();
            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
            }
            InputSystem.settings = originalSettings;
            Object.Destroy(settings);
            AudioListener.volume = originalVolume;
        }
    }

    private static async Task Press(Keyboard keyboard, Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        await Task.Delay(110);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Task.Delay(90);
    }

    private static void Click(DeathScreenButton button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Require(hits.Count > 0 && hits[0].gameObject == button.gameObject,
            "The visible button must be the actual first raycast target.");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static Services Resolve(LevelContext2D context)
    {
        var result = new Services();
        context.InjectScoped(result);
        return result;
    }

    private static void Kill()
    {
        PlayerLife2D player = Context().Player;
        Require(player.TakeDamage(player.MaximumHealth + 1000), "Lethal damage must be accepted.");
    }

    private static Task Ready() => WaitUntil(() => Phase() == DeathScreenPhase.Ready, "Death screen ready");
    private static Task Hidden() => WaitUntil(() => Phase() == DeathScreenPhase.Hidden, "Death screen hidden");

    private static async Task WaitUntil(Func<bool> condition, string label)
    {
        float deadline = Time.realtimeSinceStartup + 5;
        while (Time.realtimeSinceStartup < deadline)
        {
            try
            {
                if (condition())
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // A Single scene load briefly has no composed owner.
            }
            await Task.Delay(40);
        }
        throw new InvalidOperationException("Timed out: " + label);
    }

    private static DeathScreenPhase Phase() => Presenter().Controller.Model.Phase;
    private static DeathScreenPresenter Presenter() => All<DeathScreenPresenter>().Single();
    private static LevelContext2D Context() => All<LevelContext2D>().Single();
    private static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static void Record(string label, object details)
    {
        results.Add(new { label, frame = Time.frameCount, phase = Phase().ToString(), details });
        Save("interaction-progress.json", results);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Save(string name, object data) =>
        File.WriteAllText(Evidence + name, JsonConvert.SerializeObject(data, Formatting.Indented));
}
