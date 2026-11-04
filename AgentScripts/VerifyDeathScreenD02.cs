using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.DeathScreen.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class VerifyDeathScreenD02
{
    private const string Evidence = "Docs/Validation/DeathScreenD02/";

    public static object Start()
    {
        Require(Application.isPlaying, "Ordinary Play is required.");
        Save("native-status.json", new { status = "running" });
        One<DeathScreenEnvironment>().StartCoroutine(Guard(Capture()));
        return new { status = "scheduled" };
    }

    public static async Task<object> Retry()
    {
        DeathScreenPresenter old = One<DeathScreenPresenter>();
        string oldPlayer = One<LevelContext2D>().Player.GetEntityId().ToString();
        Require(old.Controller.Execute(DeathScreenIntent.Retry), "Retry rejected.");
        bool duplicateRejected = !old.Controller.Execute(DeathScreenIntent.Retry);
        await Task.Delay(1800);
        var result = new { oldDestroyed = old == null,
            newPlayer = One<LevelContext2D>().Player.GetEntityId().ToString() != oldPlayer,
            alive = !One<LevelContext2D>().Player.IsDead, timeScale = Time.timeScale,
            phase = One<DeathScreenPresenter>().Controller.Model.Phase.ToString(), duplicateRejected };
        Require(result.oldDestroyed && result.newPlayer && result.alive && result.timeScale == 1 &&
            result.phase == "Hidden" && duplicateRejected, "Retry must replace the scope and restore gameplay.");
        Save("native-retry.json", result);
        return result;
    }

    private static IEnumerator Guard(IEnumerator routine)
    {
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = routine.MoveNext();
                }
                catch (Exception error)
                {
                    Save("native-status.json", new { status = "failed", error = error.ToString() });
                    yield break;
                }
                if (!hasNext)
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
        var frames = new List<Texture2D>();
        var times = new List<object>();
        GameObject probe = null;
        using (var viewport = new Viewport(1280, 720))
        {
            try
            {
                float viewportDeadline = Time.realtimeSinceStartup + 5;
                while (Screen.width != 1280 || Screen.height != 720)
                {
                    Require(Time.realtimeSinceStartup < viewportDeadline, "The temporary GameView size was not applied.");
                    yield return null;
                }
                yield return new WaitForEndOfFrame();
                DeathScreenPresenter presenter = One<DeathScreenPresenter>();
                DeathScreenModel model = presenter.Controller.Model;
                LevelContext2D context = One<LevelContext2D>();
                float startedAt = Time.realtimeSinceStartup;
                Require(context.Player.TakeDamage(context.Player.MaximumHealth + 1000), "Death rejected.");
                Require(model.Phase == DeathScreenPhase.Entering && model.WorldOpacity == 1 &&
                    model.ContentOpacity == 0, "Immediate death must draw the iris and hide actions.");
                int frame = 0;
                while (true)
                {
                    yield return new WaitForEndOfFrame();
                    float seconds = Time.realtimeSinceStartup - startedAt;
                    Require(seconds < 3, "The death screen did not become ready.");
                    if (model.ClosureProgress < 1)
                    {
                        Require(model.ContentOpacity == 0 && !model.IsInteractive,
                            "Actions appeared before the world was completely black.");
                    }
                    if (frame++ % 2 == 0 || model.Phase == DeathScreenPhase.Ready)
                    {
                        frames.Add(ScreenCapture.CaptureScreenshotAsTexture());
                        times.Add(new { seconds, phase = model.Phase.ToString(), elapsed = model.Elapsed,
                            progress = model.ClosureProgress, content = model.ContentOpacity, timeScale = Time.timeScale });
                    }
                    if (model.Phase == DeathScreenPhase.Ready)
                    {
                        Require(seconds < .65f && model.ContentOpacity == 1, "The revised transition must be fast and crisp.");
                        break;
                    }
                }
                Texture2D ready = frames.Last();
                int worldSamples = 0;
                for (int y = 0; y < ready.height; y += 17)
                {
                    for (int x = ready.width * 3 / 4; x < ready.width; x += 17)
                    {
                        Color32 pixel = ready.GetPixel(x, y);
                        Require(pixel.r == 0 && pixel.g == 0 && pixel.b == 0, "The ready background leaked world pixels.");
                        worldSamples++;
                    }
                }
                var probes = new List<object>();
                probe = new GameObject("TASK006 D02 temporary pixel probe", typeof(RectTransform), typeof(Canvas));
                Canvas canvas = probe.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                RectTransform white = FullRect("White test surface", probe.transform);
                white.gameObject.AddComponent<Image>().color = Color.white;
                DeathScreenIris iris = FullRect("Actual iris graphic", probe.transform).gameObject.AddComponent<DeathScreenIris>();
                iris.raycastTarget = false;
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                int previousBlack = -1;
                foreach (float progress in new[] { 0f, .5f, 1f })
                {
                    iris.Render(progress, model.Settings.IrisFeather);
                    Canvas.ForceUpdateCanvases();
                    yield return new WaitForEndOfFrame();
                    Texture2D sample = ScreenCapture.CaptureScreenshotAsTexture();
                    try
                    {
                        Color32 center = sample.GetPixel(sample.width / 2, sample.height / 2);
                        Color32 corner = sample.GetPixel(4, 4);
                        Save("pixel-probe-last.json", new { progress, center = new[] { center.r, center.g, center.b },
                            corner = new[] { corner.r, corner.g, corner.b },
                            rect = new[] { iris.rectTransform.rect.x, iris.rectTransform.rect.y,
                                iris.rectTransform.rect.width, iris.rectTransform.rect.height } });
                        File.WriteAllBytes(Evidence + "probe-" + progress + ".png", sample.EncodeToPNG());
                        Require(corner.r == 0 && corner.g == 0 && corner.b == 0, "The corners must be black immediately.");
                        Require(progress >= 1 ? center.r == 0 : center.r == 255, "The aperture must close towards the center.");
                        int black = sample.GetPixels32().Count(pixel => pixel.r == 0 && pixel.g == 0 && pixel.b == 0);
                        Require(black > previousBlack, "Black area did not grow monotonically.");
                        Require(progress < 1 || black == sample.width * sample.height, "Full closure must be exactly black everywhere.");
                        probes.Add(new { progress, blackPixels = black, pixels = sample.width * sample.height });
                        previousBlack = black;
                    }
                    finally
                    {
                        Object.Destroy(sample);
                    }
                }
                Object.DestroyImmediate(probe);
                probe = null;
                while (Time.timeScale != 0)
                {
                    Require(Time.realtimeSinceStartup - startedAt < 2.2f, "The original delayed world pause did not complete.");
                    yield return new WaitForEndOfFrame();
                }
                float worldStoppedAt = Time.realtimeSinceStartup - startedAt;
                for (int index = 0; index < frames.Count; index++)
                {
                    File.WriteAllBytes(Evidence + "frame-" + index.ToString("D2") + ".png", frames[index].EncodeToPNG());
                }
                File.WriteAllBytes(Evidence + "ready.png", ready.EncodeToPNG());
                Save("native-transition.json", new { width = Screen.width, height = Screen.height, times,
                    worldStoppedAt, exactBlackWorldSamples = worldSamples, pixelProbe = probes,
                    audioSources = presenter.GetComponentsInChildren<AudioSource>(true).Length });
                Save("native-status.json", new { status = "completed", frames = frames.Count });
            }
            finally
            {
                if (probe != null)
                {
                    Object.DestroyImmediate(probe);
                }
                foreach (Texture2D texture in frames)
                {
                    Object.Destroy(texture);
                }
            }
        }
    }

    private static RectTransform FullRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static T One<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Save(string file, object data)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + file, JsonConvert.SerializeObject(data, Formatting.Indented));
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
            label = "TASK006-D02-" + DateTime.UtcNow.Ticks;
            object entry = Activator.CreateInstance(sizeType, Flags, null, new object[] {
                Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, label }, null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry });
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            Select(index);
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
