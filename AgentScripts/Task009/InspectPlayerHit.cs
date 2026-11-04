// Non-imported diagnostic. Execute only while this chat holds the game lease.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class InspectPlayerHit
{
    private const string Evidence = "Docs/Validation/PlayerHitTask009/";

    public static object NativeBaseline() => NativeSnapshot("native-baseline.json");

    public static object NativeFinal() => NativeSnapshot("native-final.json");

    private static object NativeSnapshot(string filename)
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            throw new InvalidOperationException("Inspect an idle stopped Editor before entering Play.");
        }
        var scenes = new List<object>();
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            GameObject[] roots = scene.GetRootGameObjects();
            Transform[] objects = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            scenes.Add(new { scene.path, scene.isDirty,
                objects = objects.Select(item => new { id = GlobalObjectId.GetGlobalObjectIdSlow(item).ToString(),
                    item.name, parent = item.parent != null ? item.parent.name : null,
                    item.gameObject.activeSelf,
                    position = new[] { item.localPosition.x, item.localPosition.y, item.localPosition.z },
                    rotation = new[] { item.localRotation.x, item.localRotation.y, item.localRotation.z, item.localRotation.w },
                    scale = new[] { item.localScale.x, item.localScale.y, item.localScale.z } }).ToArray(),
                missingScripts = objects.Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)),
                sources = Sources(scene) });
        }
        var result = new { scenes, startScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            AudioListener.volume, AudioListener.pause, Time.timeScale, Time.captureFramerate, Application.runInBackground };
        Save(filename, result);
        return new { scenes = scenes.Count, saved = Evidence + filename };
    }

    public static async Task<object> AcceptedHitBeforeIntegration()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires ordinary Play from the open level, under the game lease.");
        }
        Scene scene = SceneManager.GetActiveScene();
        int firstFrame = Time.frameCount;
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        PlayerLife2D player = context.Player;
        foreach (EnemyHealth2D enemy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            enemy.gameObject.SetActive(false);
        }
        player.GetComponent<PlayerMotor2D>().enabled = false;
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        player.SetHealthCapacity(100, 100);
        await Task.Delay(200);
        FieldInfo damage = typeof(PlayerLife2D).GetField("Damaged", BindingFlags.Instance | BindingFlags.NonPublic);
        Delegate listeners = (Delegate)damage.GetValue(player);
        object[] callbacks = listeners?.GetInvocationList().Select(item => (object)new {
            type = item.Target?.GetType().FullName, method = item.Method.Name }).ToArray() ?? Array.Empty<object>();
        object before = Sources(scene);
        string folder = Evidence + "before-integration/";
        Directory.CreateDirectory(folder);
        int previousRate = Time.captureFramerate;
        bool accepted = false;
        if (!AudioRenderer.Start())
        {
            throw new InvalidOperationException("Another audio capture is active.");
        }
        try
        {
            Time.captureFramerate = 30;
            using (var writer = new BinaryWriter(File.Create(folder + "mix.f32")))
            {
                for (int frame = 0; frame < 60; frame++)
                {
                    if (frame == 15)
                    {
                        accepted = player.TryTakeDamage(1, player.transform.position, Vector2.right);
                    }
                    await Task.Delay(33);
                    int count = AudioRenderer.GetSampleCountForCaptureFrame();
                    using (var samples = new NativeArray<float>(count * 2, Allocator.Temp))
                    {
                        if (!AudioRenderer.Render(samples))
                        {
                            throw new IOException("Native audio render failed.");
                        }
                        foreach (float value in samples)
                        {
                            writer.Write(value);
                        }
                    }
                }
            }
        }
        finally
        {
            AudioRenderer.Stop();
            Time.captureFramerate = previousRate;
        }
        var result = new { scene.path, accepted, health = player.Health, damageCallbacks = callbacks,
            sourcesBefore = before, sourcesAfter = Sources(scene),
            sampleRate = AudioSettings.outputSampleRate, channels = 2, fps = 30,
            hitFrame = 15, frames = 60, firstFrame, lastFrame = Time.frameCount,
            interpretation = "Native accepted damage, not windup or a direct HP setter. Other enemies disabled only in Play; no scene saved." };
        Save("before-integration/capture.json", result);
        return result;
    }

    private static object[] Sources(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<AudioSource>(true))
        .Select(source => (object)new { id = source.GetEntityId().ToString(), path = PathOf(source.transform),
            clip = source.clip != null ? source.clip.name : null, source.isPlaying, source.volume,
            source.pitch, source.loop, source.ignoreListenerPause,
            output = source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : null }).ToArray();

    private static string PathOf(Transform item) => item.parent == null ? item.name : PathOf(item.parent) + "/" + item.name;

    private static void Save(string filename, object value)
    {
        string path = Evidence + filename;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
    }
}
