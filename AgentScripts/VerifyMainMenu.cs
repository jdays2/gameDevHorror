using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift.Editor;
using EmergencyShift.Features.MainMenu.Logic;
using EmergencyShift.Features.MainMenu.View;
using EmergencyShift.Features.PauseMenu.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public static class VerifyMainMenu
{
    public static async Task<object> OpenHome()
    {
        var navigation = ProjectContext.Instance.Container.Resolve<IMenuNavigation>();
        navigation.Load(MainMenuAuthoring.ScenePath);
        await WaitScene(MainMenuAuthoring.ScenePath);
        return State();
    }

    public static object State()
    {
        return new { scene = SceneManager.GetActiveScene().path,
            presenters = UnityEngine.Object.FindObjectsByType<MainMenuPresenter>().Length,
            listeners = UnityEngine.Object.FindObjectsByType<AudioListener>().Length,
            events = UnityEngine.Object.FindObjectsByType<EventSystem>().Length,
            selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject?.name,
            music = UnityEngine.Object.FindAnyObjectByType<PauseMenuMusic>()?.IsPlaying,
            timeScale = Time.timeScale };
    }

    public static async Task<object> Exercise()
    {
        InputSettings original = InputSystem.settings;
        InputSettings testSettings = UnityEngine.Object.Instantiate(original);
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = testSettings;
        InputSystem.EnableDevice(Keyboard.current);
        try
        {
            return await ExerciseCore();
        }
        finally
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.settings = original;
            UnityEngine.Object.Destroy(testSettings);
        }
    }

    private static async Task<object> ExerciseCore()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Enter Play Mode first.");
        }
        var view = UnityEngine.Object.FindAnyObjectByType<MainMenuView>();
        GameObject settingsButton = view.transform.Find("Main actions/Settings").gameObject;
        EventSystem.current.SetSelectedGameObject(settingsButton);
        if (settingsButton.GetComponentInChildren<Text>().color.r < 0.9f)
        {
            throw new InvalidOperationException("Keyboard selection has no visible focus.");
        }
        GameObject quitButton = view.transform.Find("Main actions/Quit").gameObject;
        ExecuteEvents.Execute(quitButton, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
        if (EventSystem.current.currentSelectedGameObject != quitButton)
        {
            throw new InvalidOperationException("Pointer did not select its control.");
        }
        view.Request(MenuPage.Settings);
        await Task.Delay(100);
        if (!view.transform.Find("Settings").gameObject.activeSelf)
        {
            throw new InvalidOperationException("Settings page failed to open.");
        }
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape));
        await Task.Delay(100);
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        await Task.Delay(100);
        if (view.transform.Find("Settings").gameObject.activeSelf)
        {
            throw new InvalidOperationException("Escape failed to return home.");
        }
        var navigation = ProjectContext.Instance.Container.Resolve<IMenuNavigation>();
        var catalog = ProjectContext.Instance.Container.Resolve<MenuSceneCatalog>();
        string[] paths = Enumerable.Range(0, catalog.Count).Select(index => catalog.At(index).Path).ToArray();
        foreach (string path in paths)
        {
            view = UnityEngine.Object.FindAnyObjectByType<MainMenuView>();
            if (path == catalog.Find(catalog.NewGameId).Path)
            {
                view.Request(MenuPage.NewGame);
                view.transform.Find("NewGame/Content/Start").GetComponent<Button>().onClick.Invoke();
                await Task.Delay(250);
                if (SceneManager.GetActiveScene().path != catalog.HomePath)
                {
                    throw new InvalidOperationException("Scene unloaded before the music fade finished.");
                }
            }
            else
            {
                view.Request(MenuPage.Scenes);
                string title = Enumerable.Range(0, catalog.Count).Select(index => catalog.At(index))
                    .Single(scene => scene.Path == path).Title;
                Text row = view.transform.Find("Scenes/Content/Catalog rows").GetComponentsInChildren<Text>()
                    .Single(text => text.text == title);
                row.GetComponentInParent<Button>().onClick.Invoke();
            }
            await WaitScene(path);
            if (UnityEngine.Object.FindObjectsByType<MainMenuPresenter>().Length != 0)
            {
                throw new InvalidOperationException("Menu scope survived the gameplay load.");
            }
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.F8));
            await Task.Delay(100);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            await WaitScene(catalog.HomePath);
            if (UnityEngine.Object.FindObjectsByType<MainMenuPresenter>().Length != 1
                || UnityEngine.Object.FindObjectsByType<AudioListener>().Length != 1
                || UnityEngine.Object.FindObjectsByType<EventSystem>().Length != 1)
            {
                throw new InvalidOperationException("Duplicate or missing menu scope after return.");
            }
        }
        var result = new { sceneRoundTrips = paths, keyboardFocus = true, pointerFocus = true,
            escape = true, f8Return = true, singleListener = true, singleEventSystem = true,
            finalState = State() };
        Directory.CreateDirectory("work/main-menu");
        File.WriteAllText("work/main-menu/play-checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static async Task<object> MusicLoop()
    {
        var music = UnityEngine.Object.FindAnyObjectByType<PauseMenuMusic>();
        AudioSource intro = music.GetComponents<AudioSource>().Single(source => !source.loop);
        AudioSource loop = music.GetComponents<AudioSource>().Single(source => source.loop);
        intro.timeSamples = intro.clip.samples - Mathf.RoundToInt(intro.clip.frequency * 0.5f);
        await Task.Delay(2200);
        int firstSample = loop.timeSamples;
        await Task.Delay(100);
        if (!loop.isPlaying || intro.isPlaying || firstSample == loop.timeSamples)
        {
            throw new InvalidOperationException("Intro did not hand over to the running loop.");
        }
        loop.timeSamples = loop.clip.samples - 4800;
        await Task.Delay(300);
        if (!loop.isPlaying || loop.timeSamples >= loop.clip.samples - 4800)
        {
            throw new InvalidOperationException("Native loop did not wrap.");
        }
        return new { introToLoop = true, nativeWrap = true, volume = music.Config.Volume };
    }

    public static async Task<object> PausedReturn()
    {
        InputSettings original = InputSystem.settings;
        var testSettings = UnityEngine.Object.Instantiate(original);
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = testSettings;
        InputSystem.EnableDevice(Keyboard.current);
        try
        {
            var navigation = ProjectContext.Instance.Container.Resolve<IMenuNavigation>();
            const string sandbox = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            navigation.Load(sandbox);
            await WaitScene(sandbox);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape));
            await Task.Delay(100);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            await Task.Delay(100);
            if (Time.timeScale != 0 || !AudioListener.pause)
            {
                throw new InvalidOperationException("Pause did not acquire the gameplay clock and audio.");
            }
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.F8));
            await Task.Delay(100);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            await WaitScene(MainMenuAuthoring.ScenePath);
            if (Time.timeScale != 1 || AudioListener.pause
                || UnityEngine.Object.FindObjectsByType<PauseMenuMusic>().Length != 1)
            {
                throw new InvalidOperationException("Paused scene leaked its clock/audio lease or music into home.");
            }
            var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuView>();
            menu.Request(MenuPage.Quit);
            menu.transform.Find("Quit/Content/Confirm exit").GetComponent<Button>().onClick.Invoke();
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.F8));
            await Task.Delay(100);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            await Task.Delay(1000);
            if (!menu.GetComponent<CanvasGroup>().interactable
                || menu.transform.Find("Scenes").gameObject.activeSelf
                || menu.transform.Find("Quit").gameObject.activeSelf)
            {
                throw new InvalidOperationException("F8 interrupted the pending exit fade.");
            }
            return new { pausedReturn = true, timeScale = Time.timeScale, listenerPaused = AudioListener.pause,
                musicInstances = 1, quitIgnoresF8 = true };
        }
        finally
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.settings = original;
            UnityEngine.Object.Destroy(testSettings);
        }
    }

    private static async Task WaitScene(string path)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (SceneManager.GetActiveScene().path != path)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Scene transition failed: " + path);
            }
            await Task.Delay(50);
        }
        await Task.Delay(400);
    }
}

