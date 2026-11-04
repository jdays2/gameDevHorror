using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.PauseMenu.View;
using EmergencyShift.Features.PlayerHitAudio.View;
using EmergencyShift.Features.WorldAudio.Config;
using EmergencyShift.Features.WorldAudio.View;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

public static class VerifyAudioTransitions
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires ordinary Play and game access.");
        }
        var rows = new List<object>();
        LevelContext2D context = Context();
        Prepare(context);
        PlayerLife2D player = context.Player;
        var hit = player.GetComponent<PlayerHitAudioPresenter>();
        var bus = ProjectContext.Instance.Container.Resolve<WorldAudioBus>();
        var world = AssetDatabase.LoadAssetAtPath<WorldAudioProfile>("Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset");
        AudioSource[] voices = player.transform.Find("Player hit voices (runtime)").GetComponents<AudioSource>();
        Require(voices.All(source => source.outputAudioMixerGroup == world.WorldOutput), "Player hit routing");
        var uiRoot = new GameObject("TASK009 temporary independent UI audio probe");
        SceneManager.MoveGameObjectToScene(uiRoot, player.gameObject.scene);
        var ui = uiRoot.AddComponent<AudioSource>();
        ui.playOnAwake = false;
        ui.ignoreListenerPause = true;
        ui.spatialBlend = 0;
        ui.volume = .1f;
        ui.clip = AssetDatabase.LoadAssetAtPath<PlayerWorldAudioSet>("Assets/EmergencyShift/Audio/PlayerWorld/PlayerWorldAudio.asset").FlashlightSwitch;
        var lease = bus.AcquireGain();
        int originalRate = Time.captureFramerate;
        bool capturing = false;
        double worldSquare = 0;
        double uiSquare = 0;
        int worldSamples = 0;
        int uiSamples = 0;
        float worldPeak = 0;
        float uiPeak = 0;
        try
        {
            lease.Apply(0);
            await Task.Delay(200);
            int before = hit.PlayedCount;
            Require(player.TryTakeDamage(1, player.transform.position, Vector2.right), "Accepted damage while own world gain is zero");
            Require(hit.PlayedCount == before + 1, "Muted world still receives one real player cue");
            Require(AudioRenderer.Start(), "Native capture available");
            capturing = true;
            Time.captureFramerate = 30;
            for (int frame = 0; frame < 30; frame++)
            {
                if (frame == 15)
                {
                    ui.Play();
                }
                await Task.Delay(20);
                int count = AudioRenderer.GetSampleCountForCaptureFrame();
                using (var buffer = new NativeArray<float>(count * 2, Allocator.Temp))
                {
                    Require(AudioRenderer.Render(buffer), "Native DSP render");
                    foreach (float value in buffer)
                    {
                        if (frame < 15)
                        {
                            worldSquare += value * value;
                            worldPeak = Mathf.Max(worldPeak, Mathf.Abs(value));
                            worldSamples++;
                        }
                        else
                        {
                            uiSquare += value * value;
                            uiPeak = Mathf.Max(uiPeak, Mathf.Abs(value));
                            uiSamples++;
                        }
                    }
                }
            }
        }
        finally
        {
            if (capturing)
            {
                AudioRenderer.Stop();
            }
            Time.captureFramerate = originalRate;
            ui.Stop();
            Object.Destroy(uiRoot);
            lease.Dispose();
        }
        double worldRms = Math.Sqrt(worldSquare / worldSamples);
        double uiRms = Math.Sqrt(uiSquare / uiSamples);
        Require(worldRms < .00001 && worldPeak < .0001, "World route is silent at zero gain");
        Require(uiRms > .00001, "Independent UI remains audible");
        rows.Add(new { check = "native world/UI separation", worldRms, worldPeak, uiRms, uiPeak });

        foreach (bool closing in new[] { false, true })
        {
            Prepare(context);
            player = context.Player;
            hit = player.GetComponent<PlayerHitAudioPresenter>();
            var music = player.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PauseMenuMusic>(true)).Single();
            if (!music.gameObject.activeInHierarchy)
            {
                music.gameObject.SetActive(true);
            }
            music.SetMenuOpen(true);
            await Task.Delay(200);
            Require(music.IsPlaying, "Pause music started for audio-boundary test");
            if (closing)
            {
                music.SetMenuOpen(false);
                await Task.Delay(70);
                Require(music.IsPlaying, "Closing pause music tail exists before defeat");
            }
            player.RestoreHealth(1);
            int count = hit.PlayedCount;
            Require(player.TryTakeDamage(1, player.transform.position, Vector2.left), "Lethal hit accepted");
            Require(hit.PlayedCount == count + 1 && !music.IsPlaying, "Single lethal cue and synchronous music stop");
            await Task.Delay(1700);
            Require(hit.ActiveVoiceCount == 0 && bus.Gain <= .0001f, "Lethal hit completes and world fades");
            Require(!AudioListener.pause && AudioListener.volume == 1, "World fade preserves listener/UI");
            PlayerLife2D oldPlayer = player;
            AudioSource[] oldVoices = player.transform.Find("Player hit voices (runtime)").GetComponents<AudioSource>();
            string path = player.gameObject.scene.path;
            AsyncOperation reload = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            while (!reload.isDone)
            {
                await Task.Delay(30);
            }
            await Task.Delay(150);
            context = Context();
            var fresh = context.Player.GetComponent<PlayerHitAudioPresenter>();
            Require(oldPlayer == null && oldVoices.All(source => source == null), "Old player and sources destroyed on Single reload");
            Require(!context.Player.IsDead && fresh.PlayedCount == 0 && context.Player.GetComponents<PlayerHitAudioPresenter>().Length == 1,
                "Fresh scope has one silent presenter");
            Require(context.Player.transform.Find("Player hit voices (runtime)").GetComponents<AudioSource>().Length == 2 && bus.Gain > .999f,
                "Two sources without accumulation and world restored");
            rows.Add(new { check = closing ? "death during closing pause music and real reload" : "death during open pause music and real reload",
                lethalCueDelta = 1, musicStopped = true, oldScopeDestroyed = true, freshCues = fresh.PlayedCount, worldGainAfterReload = bus.Gain });
        }
        var result = new { rows, SceneManager.sceneCount, scene = SceneManager.GetActiveScene().path,
            AudioListener.volume, AudioListener.pause, Time.timeScale,
            method = "Actual new hit voices and DSP; temporary independent UI probe. Pause music audio boundary driven directly; Single reload uses the native scene loader. Existing UI intent flow belongs to TASK006." };
        File.WriteAllText("Docs/Validation/PlayerHitTask009/native-transitions.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static LevelContext2D Context() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();

    private static void Prepare(LevelContext2D context)
    {
        foreach (EnemyHealth2D enemy in context.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            enemy.gameObject.SetActive(false);
        }
        context.Player.GetComponent<PlayerMotor2D>().enabled = false;
        context.Player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        context.Player.SetHealthCapacity(100, 100);
        context.Player.RestoreHealth(100);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
