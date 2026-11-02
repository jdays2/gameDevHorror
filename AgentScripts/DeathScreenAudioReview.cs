using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using EmergencyShift.Features.WorldAudio.View;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DeathScreenAudioReview
{
    public sealed class Samples
    {
        public double rms;
        public float peak;
        public long count;
    }

    public static async Task<object> Run()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        var presenter = roots.SelectMany(root => root.GetComponentsInChildren<DeathScreenPresenter>(true)).Single();
        var audio = roots.SelectMany(root => root.GetComponentsInChildren<WorldAudioPresenter>(true)).Single();
        if (presenter.Controller.Model.Phase != DeathScreenPhase.Ready || audio.Gain != 0 ||
            Time.timeScale != 0 || AudioListener.pause)
        {
            throw new InvalidOperationException("Use the real quiet, frozen D state.");
        }
        int previousCapture = Time.captureFramerate;
        float previousVolume = AudioListener.volume;
        GameObject temporary = null;
        AudioClip clip = null;
        AudioSource source = null;
        Samples world = null;
        Samples ui = null;
        if (!AudioRenderer.Start())
        {
            throw new InvalidOperationException("Another DSP capture is active.");
        }
        try
        {
            Time.captureFramerate = 50;
            AudioListener.volume = .37f;
            await Capture(8);
            world = await Capture(8);
            temporary = new GameObject("TASK006 temporary UI audio probe");
            SceneManager.MoveGameObjectToScene(temporary, SceneManager.GetActiveScene());
            source = temporary.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.ignoreListenerPause = true;
            source.spatialBlend = 0;
            source.volume = .25f;
            var data = new float[4800];
            for (int index = 0; index < data.Length; index++)
            {
                data[index] = .2f * Mathf.Sin(2 * Mathf.PI * 200 * index / 48000);
            }
            clip = AudioClip.Create("TASK006 diagnostic tone, not a game asset", data.Length, 1, 48000, false);
            clip.SetData(data, 0);
            source.clip = clip;
            source.Play();
            await Capture(4);
            ui = await Capture(8);
            if (world.peak > .000001f || ui.rms <= .0001)
            {
                throw new InvalidOperationException("World silence or independent UI output failed.");
            }
        }
        finally
        {
            if (source != null)
            {
                source.Stop();
            }
            AudioRenderer.Stop();
            Time.captureFramerate = previousCapture;
            AudioListener.volume = previousVolume;
            if (temporary != null)
            {
                Object.Destroy(temporary);
            }
            if (clip != null)
            {
                Object.Destroy(clip);
            }
        }
        await Task.Delay(80);
        var result = new { world, independentUi = ui, worldGain = audio.Gain, Time.timeScale,
            AudioListener.pause, AudioListener.volume, originalVolume = previousVolume,
            noSavedAudioAssets = true, temporaryProbeDestroyed = temporary == null,
            deathScreenVoices = presenter.GetComponentsInChildren<AudioSource>(true).Length };
        Directory.CreateDirectory("Docs/Validation/DeathScreenD01");
        File.WriteAllText("Docs/Validation/DeathScreenD01/native-dsp.json",
            JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static async Task<Samples> Capture(int frames)
    {
        double squares = 0;
        float peak = 0;
        long count = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            await Task.Delay(20);
            using (var buffer = new NativeArray<float>(AudioRenderer.GetSampleCountForCaptureFrame() * 2, Allocator.Temp))
            {
                if (!AudioRenderer.Render(buffer))
                {
                    throw new IOException("DSP capture failed.");
                }
                foreach (float value in buffer)
                {
                    if (!float.IsFinite(value))
                    {
                        throw new InvalidDataException("Non-finite DSP sample.");
                    }
                    squares += value * value;
                    peak = Mathf.Max(peak, Mathf.Abs(value));
                    count++;
                }
            }
        }
        return new Samples { rms = Math.Sqrt(squares / count), peak = peak, count = count };
    }
}
