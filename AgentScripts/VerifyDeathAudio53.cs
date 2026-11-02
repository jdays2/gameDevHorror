using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.WorldAudio.View;
using EmergencyShift.Features.PauseMenu.Logic;
using EmergencyShift.Features.PauseMenu.View;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

public static class VerifyDeathAudio53
{
    private const string Evidence = "Docs/Validation/DeathAudio53/";

    public static object Baseline()
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(index => SceneManager.GetSceneAt(index)).ToArray();
        var result = new
        {
            play = Application.isPlaying,
            compiling = EditorApplication.isCompiling,
            startScene = EditorSceneManager.playModeStartScene,
            scenes = scenes.Select(scene => new { scene.path, scene.isDirty, scene.isLoaded }),
            sources = scenes.SelectMany(scene => scene.GetRootGameObjects()).SelectMany(root => root.GetComponentsInChildren<AudioSource>(true))
                .Select(source => new { path = PathOf(source.transform), source.volume, source.mute, source.ignoreListenerPause,
                    mixer = source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : null,
                    clip = source.clip != null ? source.clip.name : null }),
            listenerVolume = AudioListener.volume,
            listenerPause = AudioListener.pause,
            timeScale = Time.timeScale
        };
        Save("native-baseline.json", result);
        return result;
    }

    public static object FinalState()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires an idle EditMode Editor.");
        }
        var scene = SceneManager.GetActiveScene();
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var sources = objects.SelectMany(item => item.GetComponents<AudioSource>()).ToArray();
        int missingScripts = objects.Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
        var profile = AssetDatabase.LoadAssetAtPath<EmergencyShift.Features.WorldAudio.Config.WorldAudioProfile>("Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset");
        profile.Validate();
        profile.WorldOutput.audioMixer.GetFloat(profile.AttenuationParameter, out float decibels);
        var result = new { scene.path, scene.isDirty, loadedScenes = SceneManager.sceneCount,
            playModeStartScene = EditorSceneManager.playModeStartScene,
            missingScripts, sources = sources.Length, savedWorldRoutes = sources.Count(source => source.outputAudioMixerGroup != null),
            savedRuntimePresenters = objects.Sum(item => item.GetComponents<WorldAudioPresenter>().Length),
            mixerDecibels = decibels, AudioListener.volume, AudioListener.pause, Time.timeScale,
            authoringGate = "Not applicable: no authored scene objects added/moved; runtime presentation/probes were not saved" };
        Save("native-final.json", result);
        if (scene.isDirty || SceneManager.sceneCount != 1 || EditorSceneManager.playModeStartScene != null ||
            missingScripts != 0 || result.savedWorldRoutes != 0 || result.savedRuntimePresenters != 0)
        {
            throw new InvalidOperationException("Final native preservation check failed.");
        }
        return result;
    }

    public static Task<object> Reproduce() => Capture("before", false);

    public static Task<object> After() => Capture("after", true);

    public static Task<object> AfterMorgue() => Capture("morgue-after", true);

    private static async Task<object> Capture(string label, bool freezeAtDeath)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires ordinary Play from the open level.");
        }

        var context = Object.FindAnyObjectByType<LevelContext2D>();
        PlayerLife2D player = context.Player;
        var lamps = context.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LocalLampAudio2D>(true)).ToArray();
        LocalLampAudio2D lamp = lamps.OrderBy(value => Vector3.Distance(value.transform.position, player.transform.position)).First();
        player.GetComponent<PlayerMotor2D>().enabled = false;
        foreach (var enemy in Object.FindObjectsByType<ShadowStalker2D>())
        {
            enemy.AttacksEnabled = false;
        }
        player.GetComponent<Rigidbody2D>().position = (Vector2)lamp.transform.position + Vector2.down;
        Physics2D.SyncTransforms();
        await Task.Delay(1000);

        int previousCapture = Time.captureFramerate;
        float previousScale = Time.timeScale;
        var windows = new List<object>();
        string file = Evidence + label + "-world.f32";
        var allSources = context.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AudioSource>(true)).ToArray();
        var uiRoot = new GameObject("Temporary independent UI audio probe");
        SceneManager.MoveGameObjectToScene(uiRoot, context.gameObject.scene);
        var ui = uiRoot.AddComponent<AudioSource>();
        ui.playOnAwake = false;
        ui.spatialBlend = 0;
        ui.ignoreListenerPause = true;
        ui.volume = .25f;
        var door = Object.FindAnyObjectByType<Door2D>();
        GameObject spawned = null;
        var newWorldEvents = new List<object>();
        float unscaledStart = Time.unscaledTime;
        Directory.CreateDirectory(Evidence);
        if (!AudioRenderer.Start())
        {
            throw new InvalidOperationException("Another audio capture is active.");
        }
        try
        {
            Time.captureFramerate = 30;
            using (var writer = new BinaryWriter(File.Create(file)))
            {
                for (int frame = 0; frame < 150; frame++)
                {
                    if (frame == 30)
                    {
                        if (!player.TakeDamage(player.MaximumHealth + 100))
                        {
                            throw new InvalidOperationException("Staged lethal damage was rejected.");
                        }
                        if (freezeAtDeath)
                        {
                            Time.timeScale = 0;
                        }
                    }
                    if (frame == 90)
                    {
                        Time.timeScale = 0;
                    }
                    if (freezeAtDeath && frame == 100)
                    {
                        door.Toggle();
                        newWorldEvents.Add(new { frame, kind = "authored door begins motion after silence" });
                    }
                    if (freezeAtDeath && frame == 105)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
                        spawned = context.CreateInstance(prefab, player.transform.position + Vector3.right, Quaternion.identity);
                        spawned.GetComponent<ShadowStalker2D>().Kill();
                        newWorldEvents.Add(new { frame, kind = "new DI-composed creature rupture after silence",
                            allVoicesRouted = spawned.GetComponentsInChildren<AudioSource>(true).All(source => source.outputAudioMixerGroup != null) });
                    }
                    if (freezeAtDeath && frame == 110)
                    {
                        var reader = Object.FindObjectsByType<DoorAccessFeedback2D>().FirstOrDefault();
                        if (reader != null)
                        {
                            player.transform.position = reader.transform.position + Vector3.down;
                            Physics2D.SyncTransforms();
                            reader.GetComponent<DoorAccess2D>().TryUnlock(player.GetComponent<PlayerInteraction>());
                            newWorldEvents.Add(new { frame, kind = "card reader feedback after silence",
                                voices = reader.GetComponentsInChildren<AudioSource>(true).Select(source => new { source.isPlaying, source.volume }).ToArray() });
                        }
                    }
                    if (freezeAtDeath && frame == 120)
                    {
                        ui.clip = lamp.GetComponentsInChildren<AudioSource>(true).First(source => source.clip != null).clip;
                        ui.Play();
                    }
                    await Task.Delay(33);
                    int count = AudioRenderer.GetSampleCountForCaptureFrame();
                    using (var buffer = new NativeArray<float>(count * 2, Allocator.Temp))
                    {
                        if (!AudioRenderer.Render(buffer))
                        {
                            throw new IOException("Main mix capture failed.");
                        }
                        double square = 0;
                        float peak = 0;
                        foreach (float sample in buffer)
                        {
                            writer.Write(sample);
                            square += sample * sample;
                            peak = Mathf.Max(peak, Mathf.Abs(sample));
                        }
                        if (frame % 10 == 0)
                        {
                            windows.Add(new { frame, captureSeconds = frame / 30f, player.IsDead, Time.timeScale,
                                unscaledSeconds = Time.unscaledTime - unscaledStart,
                                rms = Math.Sqrt(square / buffer.Length), peak,
                                worldGain = player.GetComponent<WorldAudioPresenter>()?.Gain,
                                uiPlaying = ui.isPlaying,
                                sources = lamp.GetComponentsInChildren<AudioSource>(true).Select(source => new
                                {
                                    source.name, source.isPlaying, source.volume,
                                    clip = source.clip != null ? source.clip.name : null
                                }).ToArray() });
                        }
                    }
                }
            }
        }
        finally
        {
            AudioRenderer.Stop();
            Time.captureFramerate = previousCapture;
            Time.timeScale = previousScale;
            Object.Destroy(uiRoot);
            if (spawned != null)
            {
                Object.Destroy(spawned);
            }
        }
        var result = new { scene = context.gameObject.scene.path, lamp = PathOf(lamp.transform), lethalAt = 1,
            timeStoppedAt = freezeAtDeath ? 1 : 3, sampleRate = AudioSettings.outputSampleRate, channels = 2, frames = 150, file, windows, newWorldEvents,
            routes = allSources.Select(source => new { path = PathOf(source.transform),
                output = source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : null,
                ui = source.GetComponentInParent<Canvas>(true) != null || source.GetComponent<EmergencyShift.Features.PauseMenu.View.PauseMenuMusic>() != null }),
            method = "Controlled native Play: staged player beside authored lamp, real lethal damage, AudioRenderer main mix; no saved scene edits." };
        Save(label + "-reproduction.json", result);
        return result;
    }

    public static async Task<object> Lifecycle()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires ordinary Play.");
        }
        var checks = new List<string>();
        var context = Object.FindAnyObjectByType<LevelContext2D>();
        PlayerLife2D player = context.Player;
        var dependencies = new Dependencies();
        context.InjectScoped(dependencies);
        var music = Object.FindAnyObjectByType<PauseMenuMusic>();
        var presentation = player.GetComponent<WorldAudioPresenter>();
        float listenerVolume = AudioListener.volume;
        bool listenerPause = AudioListener.pause;
        float scale = Time.timeScale;
        try
        {
            AudioListener.volume = .37f;
            player.RestoreHealth(player.MaximumHealth);
            var lamp = Object.FindObjectsByType<LocalLampAudio2D>().OrderBy(value => Vector3.Distance(value.transform.position, player.transform.position)).First();
            player.transform.position = lamp.transform.position + Vector3.down;
            Physics2D.SyncTransforms();
            foreach (var enemy in Object.FindObjectsByType<ShadowStalker2D>())
            {
                enemy.AttacksEnabled = false;
            }
            await Task.Delay(1000);
            Check(presentation.Gain == 1, "restore returns to unit gain", checks);
            float restoredPeak = await OutputPeak();
            Check(restoredPeak > .000001f, "restored world produces real output samples", checks);
            var participant = player.GetComponent<PlayerCheckpoint2D>();
            string payload = participant != null ? participant.CaptureCheckpoint() : null;

            Check(dependencies.Pause.TryOpen(), "ordinary pause opens", checks);
            await Task.Delay(400);
            Check(music.IsPlaying && AudioListener.pause, "open pause music plays independently", checks);
            Check(player.TakeDamage(player.MaximumHealth + 100), "lethal damage accepted while pause is open", checks);
            Check(!music.IsPlaying, "death stops open pause music immediately", checks);
            dependencies.Pause.Cancel();
            Check(!AudioListener.pause, "existing pause Cancel releases its own audio pause", checks);
            Time.timeScale = 0;
            await Task.Delay(1650);
            Check(presentation.Gain == 0, "death fade finishes while time is stopped", checks);
            if (participant != null)
            {
                participant.RestoreCheckpoint(payload);
            }
            else
            {
                player.RestoreHealth(player.MaximumHealth);
            }
            await Task.Delay(500);
            Check(presentation.Gain == 1 && !player.IsDead, "checkpoint health restore recovers audio while time is stopped", checks);
            Check(AudioListener.volume == .37f, "user master volume remains unchanged", checks);

            Time.timeScale = 1;
            Check(dependencies.Pause.TryOpen(), "pause can open again", checks);
            await Task.Delay(400);
            Check(dependencies.Pause.TryResume(), "pause resumes", checks);
            Check(music.IsPlaying, "closing pause has a live fade tail before death", checks);
            Check(player.TakeDamage(player.MaximumHealth + 100), "second lethal damage accepted", checks);
            Check(!music.IsPlaying && !dependencies.Pause.IsOpen, "death cancels closing pause music", checks);
            await Task.Delay(1650);
            Check(presentation.Gain == 0, "second death reaches silence", checks);
            player.RestoreHealth(player.MaximumHealth);
            await Task.Delay(500);
            Check(presentation.Gain == 1, "second restore recovers without accumulated attenuation", checks);

            AudioListener.pause = true;
            Check(player.TakeDamage(player.MaximumHealth + 100), "death with external audio pause accepted", checks);
            await Task.Delay(1650);
            Check(presentation.Gain == 0 && AudioListener.pause, "external audio pause is preserved while envelope finishes", checks);
            AudioListener.pause = false;
            Check(presentation.Gain == 0, "external unpause cannot reveal world audio after death", checks);
            player.RestoreHealth(player.MaximumHealth);
            await Task.Delay(500);
            Check(presentation.Gain == 1, "third cycle restores full world gain", checks);

            player.TakeDamage(player.MaximumHealth + 100);
            await Task.Delay(1650);
            player.RestartRoom();
            await Task.Delay(1200);
            var next = Object.FindAnyObjectByType<LevelContext2D>();
            Check(next != null && next != context && !next.Player.IsDead, "actual restart unloads old player scope", checks);
            Check(next.Player.GetComponent<WorldAudioPresenter>().Gain == 1, "new scene starts without stale mute", checks);
            Check(AudioListener.volume == .37f && !AudioListener.pause && Time.timeScale == 1,
                "restart preserves user volume and normal pause/time state", checks);
            var result = new { passed = true, scene = next.gameObject.scene.path, checks,
                checkpointParticipantPresent = participant != null,
                checkpointScope = participant != null ? "Real PlayerCheckpoint2D payload restoration; no disk slot/controller added" : "Real PlayerLife2D trusted restoration path; no checkpoint controller in this scene",
                cycles = 4, userMasterVolume = .37f, restoredPeak };
            Save("lifecycle.json", result);
            return result;
        }
        finally
        {
            AudioListener.pause = listenerPause;
            AudioListener.volume = listenerVolume;
            Time.timeScale = scale;
        }
    }

    public static async Task<object> PersistenceRestore()
    {
        var context = Object.FindAnyObjectByType<LevelContext2D>();
        PlayerLife2D player = context.Player;
        var participant = player.GetComponent<PlayerCheckpoint2D>();
        bool temporary = participant == null;
        WeaponCatalog temporaryCatalog = null;
        if (temporary)
        {
            participant = player.gameObject.AddComponent<PlayerCheckpoint2D>();
            temporaryCatalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            using (var data = new SerializedObject(temporaryCatalog))
            {
                var definitions = data.FindProperty("weapons");
                definitions.arraySize = 2;
                definitions.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
                definitions.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PumpShotgun.asset");
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            participant.Configure(temporaryCatalog);
        }
        float previousScale = Time.timeScale;
        var probeRoot = new GameObject("Temporary world audio restore probe");
        SceneManager.MoveGameObjectToScene(probeRoot, context.gameObject.scene);
        var probe = probeRoot.AddComponent<AudioSource>();
        var bindings = new Dependencies();
        context.InjectScoped(bindings);
        bindings.Bus.Route(probe);
        var lamp = Object.FindAnyObjectByType<LocalLampAudio2D>();
        probe.clip = lamp.GetComponentsInChildren<AudioSource>(true).First(source => source.clip != null).clip;
        probe.loop = true;
        probe.volume = .25f;
        probe.Play();
        try
        {
            string payload = participant.CaptureCheckpoint();
            participant.ValidateCheckpoint(payload);
            string isolatedPath = Path.GetFullPath("../../work/bug053/player-payload.json");
            File.WriteAllText(isolatedPath, payload);
            player.TakeDamage(player.MaximumHealth + 100);
            Time.timeScale = 0;
            await Task.Delay(1650);
            var presentation = player.GetComponent<WorldAudioPresenter>();
            if (presentation.Gain != 0)
            {
                throw new InvalidOperationException("World did not reach silence before checkpoint payload restoration.");
            }
            participant.RestoreCheckpoint(File.ReadAllText(isolatedPath));
            await Task.Delay(500);
            float restoredPeak = await OutputPeak();
            bool passed = !player.IsDead && presentation.Gain == 1 && restoredPeak > .000001f;
            var result = new { passed, isolatedPath, temporaryPlayerParticipant = temporary,
                sceneCheckpointControllerAdded = false, actualNativeRestore = "PlayerCheckpoint2D.RestoreCheckpoint → PlayerLife2D.RestoreHealth",
                player.Health, worldGain = presentation.Gain, restoredPeak, preservedSourceLevel = probe.volume,
                sampleProbe = "Temporary world-routed source with existing lamp WAV, fixed level .25; no scene save", Time.timeScale };
            Save("player-payload-restore.json", result);
            if (!passed)
            {
                throw new InvalidOperationException("Native checkpoint payload failed to restore audio.");
            }
            return result;
        }
        finally
        {
            Time.timeScale = previousScale;
            Object.Destroy(probeRoot);
            if (temporary)
            {
                Object.Destroy(participant);
                Object.Destroy(temporaryCatalog);
            }
        }
    }

    public static async Task<object> RestartAfterDeath()
    {
        var previous = Object.FindAnyObjectByType<LevelContext2D>();
        string path = previous.gameObject.scene.path;
        previous.Player.RestartRoom();
        await Task.Delay(1200);
        var current = Object.FindAnyObjectByType<LevelContext2D>();
        bool passed = current != null && current != previous && !current.Player.IsDead &&
            current.Player.GetComponent<WorldAudioPresenter>().Gain == 1;
        var result = new { passed, scene = path, previousScopeDestroyed = previous == null,
            worldGain = current.Player.GetComponent<WorldAudioPresenter>().Gain, AudioListener.volume, AudioListener.pause, Time.timeScale };
        Save("morgue-unload.json", result);
        if (!passed)
        {
            throw new InvalidOperationException("Reload retained a defeated audio scope.");
        }
        return result;
    }

    private static void Check(bool condition, string label, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
        checks.Add(label);
    }

    private static async Task<float> OutputPeak()
    {
        int previousCapture = Time.captureFramerate;
        if (!AudioRenderer.Start())
        {
            throw new InvalidOperationException("Another audio capture is active.");
        }
        float peak = 0;
        try
        {
            Time.captureFramerate = 30;
            for (int frame = 0; frame < 10; frame++)
            {
                await Task.Delay(33);
                using (var buffer = new NativeArray<float>(AudioRenderer.GetSampleCountForCaptureFrame() * 2, Allocator.Temp))
                {
                    if (!AudioRenderer.Render(buffer))
                    {
                        throw new IOException("Restored output capture failed.");
                    }
                    foreach (float sample in buffer)
                    {
                        peak = Mathf.Max(peak, Mathf.Abs(sample));
                    }
                }
            }
        }
        finally
        {
            AudioRenderer.Stop();
            Time.captureFramerate = previousCapture;
        }
        return peak;
    }

    public sealed class Dependencies
    {
        public PauseMenuController Pause;
        public WorldAudioBus Bus;
        [Inject]
        public void Construct(PauseMenuController pause, WorldAudioBus bus)
        {
            Pause = pause;
            Bus = bus;
        }
    }

    private static string PathOf(Transform item) => item.parent != null ? PathOf(item.parent) + "/" + item.name : item.name;

    private static void Save(string file, object value)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + file, JsonConvert.SerializeObject(value, Formatting.Indented));
    }
}
