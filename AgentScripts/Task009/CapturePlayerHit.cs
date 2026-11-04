using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.PlayerHitAudio.Config;
using EmergencyShift.Features.PlayerHitAudio.View;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class CapturePlayerHit
{
    public static Task<object> RecordD() => Record("D");
    public static Task<object> RecordE() => Record("E");
    public static Task<object> RecordF() => Record("F");
    public static Task<object> RecordJ() => Record("J");
    public static Task<object> RecordK() => Record("K");
    public static Task<object> RecordL() => Record("L");

    public static Task<object> Record(string candidate)
    {
        if (!Application.isPlaying || !new[] { "A", "B", "C", "D", "E", "F", "J", "K", "L" }.Contains(candidate))
        {
            throw new InvalidOperationException("Requires ordinary Play and candidate A/B/C/D/E/F/J/K/L.");
        }
        var viewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        EditorWindow view = EditorWindow.GetWindow(viewType);
        view.Show();
        view.Focus();
        var owner = new GameObject("TASK009 temporary capture");
        return owner.AddComponent<PlayerHitCaptureProbe>().Run(candidate);
    }
}

public sealed class PlayerHitCaptureProbe : MonoBehaviour
{
    private readonly TaskCompletionSource<object> completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<AudioClip> temporaryClips = new List<AudioClip>();
    private readonly List<object> damageEvents = new List<object>();
    private PlayerLife2D player;
    private PlayerHitAudioPresenter hit;
    private PlayerAim2D aim;
    private PlayerHitAudioProfile original;
    private PlayerHitAudioProfile audition;
    private FieldInfo profileField;
    private BinaryWriter writer;
    private int priorRate;
    private int currentFrame;
    private bool recording;
    private bool cleaned;
    private Viewport viewport;

    public Task<object> Run(string candidate)
    {
        StartCoroutine(Guard(Capture(candidate)));
        return completion.Task;
    }

    private IEnumerator Guard(IEnumerator process)
    {
        while (true)
        {
            bool moved = false;
            Exception failure = null;
            try
            {
                moved = process.MoveNext();
            }
            catch (Exception error)
            {
                failure = error;
            }
            if (failure != null)
            {
                Cleanup();
                completion.TrySetException(failure);
                break;
            }
            if (!moved)
            {
                break;
            }
            yield return process.Current;
        }
        Destroy(gameObject);
    }

    private IEnumerator Capture(string candidate)
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        player = context.Player;
        foreach (EnemyHealth2D enemy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            enemy.gameObject.SetActive(false);
        }
        player.GetComponent<PlayerMotor2D>().enabled = false;
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        player.SetHealthCapacity(100, 100);
        context.Flashlight.SetPower(false);
        viewport = new Viewport(1280, 720);
        for (int wait = 0; wait < 60 && (Screen.width != 1280 || Screen.height != 720); wait++)
        {
            yield return new WaitForEndOfFrame();
        }
        hit = player.GetComponent<PlayerHitAudioPresenter>();
        profileField = typeof(PlayerHitAudioPresenter).GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic);
        original = (PlayerHitAudioProfile)profileField.GetValue(hit);
        string unchanged = JsonUtility.ToJson(original);
        if (candidate != "B")
        {
            audition = Object.Instantiate(original);
            using (var data = new SerializedObject(audition))
            {
                SerializedProperty clips = data.FindProperty("clips");
                bool singleSourceAudition = new[] { "D", "E", "F", "J", "K", "L" }.Contains(candidate);
                if (singleSourceAudition)
                {
                    clips.arraySize = 1;
                }
                for (int index = 0; index < clips.arraySize; index++)
                {
                    string path;
                    if (singleSourceAudition)
                    {
                        string revision = "Bite02";
                        if (new[] { "J", "K", "L" }.Contains(candidate))
                        {
                            revision = "Reaction04";
                        }
                        path = "Concepts/Unused/Audio/PlayerHitTask009/" + revision + "/Exports/" + candidate + "_hit.wav";
                    }
                    else
                    {
                        path = "Concepts/Unused/Audio/PlayerHitTask009/Exports/" + candidate + "/player-hit-" + (index + 1).ToString("D2") + ".wav";
                    }
                    AudioClip clip = ReadClip(path);
                    temporaryClips.Add(clip);
                    clips.GetArrayElementAtIndex(index).objectReferenceValue = clip;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            audition.Validate();
            profileField.SetValue(hit, audition);
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        LayerMask walls = prefab.GetComponent<ShadowStalker2D>().BlockingGeometry;
        Vector2[] options = { Vector2.right, Vector2.up, Vector2.down, Vector2.left };
        Vector2 direction = options.First(offset => Physics2D.OverlapCircle((Vector2)player.transform.position + offset * 1.2f, .35f, walls) == null &&
            Physics2D.Linecast(player.transform.position, (Vector2)player.transform.position + offset * 1.2f, walls).collider == null);
        GameObject actor = context.CreateInstance(prefab, player.transform.position + (Vector3)direction * 1.2f, Quaternion.identity);
        aim = player.GetComponent<PlayerAim2D>();
        aim.SetScriptedTarget(actor.transform.position);
        var weapon = player.GetComponent<PlayerWeapon2D>();
        player.Damaged += OnDamage;
        yield return new WaitForEndOfFrame();
        string folder = "work/task009-player-hit/capture-" + candidate + "-hits-only-live/";
        if (Directory.Exists(folder))
        {
            throw new IOException("Preserve an existing capture before recording again.");
        }
        Directory.CreateDirectory(folder);
        var frames = new List<object>();
        int shotsBefore = weapon.ShotsFired;
        int cuesBefore = hit.PlayedCount;
        priorRate = Time.captureFramerate;
        double capturedSeconds = 0;
        double stoppedAt = 0;
        bool stoppedAttacks = false;
        object result = null;
        try
        {
            if (!AudioRenderer.Start())
            {
                throw new InvalidOperationException("Another audio capture is active.");
            }
            recording = true;
            Time.captureDeltaTime = 1f / 60;
            writer = new BinaryWriter(File.Create(folder + "mix.f32"));
            for (int frame = 0; frame < 900 && capturedSeconds < 12; frame++)
            {
                currentFrame = frame;
                yield return new WaitForEndOfFrame();
                Time.captureDeltaTime = Mathf.Max(1f / 1000, Time.unscaledDeltaTime);
                if (!stoppedAttacks && hit.PlayedCount - cuesBefore >= 4)
                {
                    actor.GetComponent<ShadowStalker2D>().AttacksEnabled = false;
                    stoppedAttacks = true;
                    stoppedAt = capturedSeconds;
                }
                int count = AudioRenderer.GetSampleCountForCaptureFrame();
                capturedSeconds += (double)count / AudioSettings.outputSampleRate;
                using (var buffer = new NativeArray<float>(count * 2, Allocator.Temp))
                {
                    if (!AudioRenderer.Render(buffer))
                    {
                        throw new IOException("Native mix render failed.");
                    }
                    foreach (float value in buffer)
                    {
                        writer.Write(value);
                    }
                }
                Texture2D pixels = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(folder + frame.ToString("D4") + ".png", pixels.EncodeToPNG());
                Object.Destroy(pixels);
                frames.Add(new { frame, gameFrame = Time.frameCount, Time.time, Time.unscaledTime,
                    Time.deltaTime, Time.unscaledDeltaTime, dspTime = AudioSettings.dspTime,
                    audioSamples = count, player.Health, hit.PlayedCount, hit.ActiveVoiceCount });
                if (stoppedAttacks && capturedSeconds >= stoppedAt + 1.2)
                {
                    break;
                }
            }
            if (weapon.ShotsFired != shotsBefore || hit.PlayedCount - cuesBefore != 4)
            {
                throw new InvalidOperationException("Audition requires exactly four accepted hits and zero shots.");
            }
            result = new { candidate, scene = scene.path, fps = 30, channels = 2,
                sampleRate = AudioSettings.outputSampleRate, frames = frames.Count,
                width = Screen.width, height = Screen.height, clips = damageEvents,
                shots = weapon.ShotsFired - shotsBefore, hit.PlayedCount, framesObserved = frames,
                profileVolume = original.Volume, profilePitchVariation = original.PitchVariation,
                auditionClipCount = audition != null ? audition.Clips.Count : original.Clips.Count,
                source = "Native AudioRenderer main mix and composited GameView, no normalization or substituted audio.",
                fixture = "Four spaced accepted hits by an autonomous ShadowStalker, no firing. Player held at original spawn with temporary 100HP and aim at the enemy; other enemies disabled only during Play. Attacks stopped after hit four, with a 1.2s tail. Attack timing/damage/assets unchanged.",
                auditionBank = new[] { "D", "E", "F", "J", "K", "L" }.Contains(candidate)
                    ? "One source for this audition, repeated on four real hits; not a completed variation bank. Only the memory-copy clip bank changes; production gain/pitch/routing retained."
                    : "Existing B bank, or historical A/C audition variants.",
                originalProfileUnchanged = JsonUtility.ToJson(original) == unchanged,
                captureTiming = "Per-frame audio sample count follows unscaled time; video uses those sample durations." };
        }
        finally
        {
            Cleanup();
        }
        File.WriteAllText(folder + "capture.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        completion.TrySetResult(result);
    }

    private void OnDamage(int amount, Vector2 direction)
    {
        damageEvents.Add(new { frame = currentFrame, amount, gameTime = Time.time, unscaledTime = Time.unscaledTime,
            clip = hit.LastClip != null ? hit.LastClip.name : null });
    }

    private void OnDestroy() => Cleanup();

    private void Cleanup()
    {
        if (cleaned)
        {
            return;
        }
        cleaned = true;
        writer?.Dispose();
        writer = null;
        if (recording)
        {
            AudioRenderer.Stop();
            Time.captureFramerate = priorRate;
            recording = false;
        }
        if (player != null)
        {
            player.Damaged -= OnDamage;
        }
        if (aim != null)
        {
            aim.ClearScriptedTarget();
        }
        if (hit != null && audition != null)
        {
            hit.Dispose();
            profileField.SetValue(hit, original);
        }
        foreach (AudioClip clip in temporaryClips)
        {
            Object.Destroy(clip);
        }
        if (audition != null)
        {
            Object.Destroy(audition);
        }
        viewport?.Dispose();
    }

    private static AudioClip ReadClip(string path)
    {
        using (var reader = new BinaryReader(File.OpenRead(path)))
        {
            reader.ReadBytes(12);
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string chunk = Encoding.ASCII.GetString(reader.ReadBytes(4));
                int size = reader.ReadInt32();
                if (chunk == "data")
                {
                    var samples = new float[size / 2];
                    for (int index = 0; index < samples.Length; index++)
                    {
                        samples[index] = reader.ReadInt16() / 32768f;
                    }
                    var clip = AudioClip.Create("TASK009 audition " + Path.GetFileNameWithoutExtension(path), samples.Length, 1, 48000, false);
                    clip.SetData(samples, 0);
                    return clip;
                }
                reader.BaseStream.Position += size + size % 2;
            }
        }
        throw new InvalidDataException("Missing PCM16 data: " + path);
    }

    private sealed class Viewport : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        private readonly Type viewType;
        private readonly Type sizeType;
        private readonly EditorWindow view;
        private readonly object group;
        private readonly int originalIndex;
        private readonly string label;

        public Viewport(int width, int height)
        {
            Assembly editor = typeof(Editor).Assembly;
            viewType = editor.GetType("UnityEditor.GameView");
            sizeType = editor.GetType("UnityEditor.GameViewSize");
            view = EditorWindow.GetWindow(viewType);
            originalIndex = (int)viewType.GetProperty("selectedSizeIndex", Flags).GetValue(view);
            object sizes = editor.GetType("UnityEditor.GameViewSizes").GetProperty("instance", Flags).GetValue(null);
            group = sizes.GetType().GetProperty("currentGroup", Flags).GetValue(sizes);
            label = "TASK009-" + DateTime.UtcNow.Ticks;
            object entry = Activator.CreateInstance(sizeType, Flags, null, new object[] {
                Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, label }, null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry });
            Select((int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1);
        }

        public void Dispose()
        {
            Select(originalIndex);
            int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            for (int index = total - 1; index >= 0; index--)
            {
                object entry = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { index });
                if ((string)sizeType.GetProperty("baseText", Flags).GetValue(entry) == label)
                {
                    group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
                }
            }
        }

        private void Select(int index)
        {
            viewType.GetMethod("SizeSelectionCallback", Flags).Invoke(view, new object[] { index, null });
            view.Repaint();
        }
    }
}
