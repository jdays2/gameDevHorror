using System;
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
using Object = UnityEngine.Object;

public static class DeathScreenLayoutReview
{
    private const string Evidence = "Docs/Validation/DeathScreenD01/";
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
        BindingFlags.Static | BindingFlags.FlattenHierarchy;

    public static object CleanupFirstRunSizes()
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        Type entryType = assembly.GetType("UnityEditor.GameViewSize");
        object sizes = sizesType.GetProperty("instance", Flags).GetValue(null);
        object group = sizesType.GetProperty("currentGroup", Flags).GetValue(sizes);
        Type groupType = group.GetType();
        int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        int builtin = (int)groupType.GetMethod("GetBuiltinCount").Invoke(group, null);
        var names = new HashSet<string>(new[] { "TASK006-QA-1280x720", "TASK006-QA-1920x1080",
            "TASK006-QA-2560x1440", "TASK006-QA-2560x1080", "TASK006-QA-1024x768" });
        var removed = new List<string>();
        for (int index = total - 1; index >= builtin; index--)
        {
            object entry = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { index });
            string label = (string)entryType.GetProperty("baseText", Flags).GetValue(entry);
            if (names.Contains(label))
            {
                groupType.GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
                removed.Add(label);
            }
        }
        int after = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        return new { before = total, after, removed };
    }

    public static object Start()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Use Play with an installed death screen.");
        }
        Directory.CreateDirectory(Evidence);
        Save("layout-status.json", new { status = "running" });
        EditorApplication.delayCall += async () =>
        {
            try
            {
                object result = await Run();
                Save("native-layout.json", result);
                Save("layout-status.json", new { status = "completed" });
            }
            catch (Exception error)
            {
                Save("layout-status.json", new { status = "failed", error = error.ToString() });
            }
        };
        return new { status = "scheduled" };
    }

    private static async Task<object> Run()
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type viewType = assembly.GetType("UnityEditor.GameView");
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
        Type sizeKind = assembly.GetType("UnityEditor.GameViewSizeType");
        var view = EditorWindow.GetWindow(viewType);
        object sizes = sizesType.GetProperty("instance", Flags).GetValue(null);
        object group = sizesType.GetProperty("currentGroup", Flags).GetValue(sizes);
        Type groupType = group.GetType();
        var selected = viewType.GetProperty("selectedSizeIndex", Flags);
        int originalSelection = (int)selected.GetValue(view);
        int originalCount = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        var ownedLabels = new HashSet<string>();
        var captures = new List<object>();
        var failures = new List<object>();
        DeathScreenPresenter presenter = Presenter();
        var texts = presenter.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        int[] fontSizes = texts.Select(text => text.fontSize).ToArray();
        try
        {
            foreach (Vector2Int dimensions in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1440), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
            {
                string label = "TASK006-QA-" + DateTime.UtcNow.Ticks + "-" + dimensions.x + "x" + dimensions.y;
                object entry = Activator.CreateInstance(sizeType, Flags, null, new[]
                {
                    Enum.Parse(sizeKind, "FixedResolution"), (object)dimensions.x, dimensions.y,
                    label
                }, null);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { entry });
                ownedLabels.Add(label);
                int index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                viewType.GetMethod("SizeSelectionCallback", Flags).Invoke(view, new object[] { index, null });
                view.Repaint();
                await Until(() => Screen.width == dimensions.x && Screen.height == dimensions.y, "Actual GameView size");
                foreach (float scale in new[] { 1f, 1.25f })
                {
                    await Ready(presenter);
                    for (int item = 0; item < texts.Length; item++)
                    {
                        texts[item].fontSize = Mathf.RoundToInt(fontSizes[item] * scale);
                    }
                    await Task.Delay(80);
                    captures.Add(await Capture("ready", scale, failures));
                    Require(presenter.Controller.Execute(DeathScreenIntent.AskQuit), "Quit confirmation should open.");
                    await Task.Delay(60);
                    captures.Add(await Capture("confirmation", scale, failures));
                    Require(presenter.Controller.Execute(DeathScreenIntent.ConfirmQuit), "Quit should enter its transition.");
                    await Until(() => presenter.Controller.Model.Phase == DeathScreenPhase.Error, "Editor exit message");
                    await Task.Delay(70);
                    captures.Add(await Capture("error", scale, failures));
                }
            }
            await Ready(presenter);
        }
        finally
        {
            for (int item = 0; item < texts.Length; item++)
            {
                if (texts[item] != null)
                {
                    texts[item].fontSize = fontSizes[item];
                }
            }
            selected.SetValue(view, originalSelection);
            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int builtin = (int)groupType.GetMethod("GetBuiltinCount").Invoke(group, null);
            for (int index = total - 1; index >= builtin; index--)
            {
                object entry = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { index });
                string label = (string)sizeType.GetProperty("baseText", Flags).GetValue(entry);
                if (ownedLabels.Contains(label))
                {
                    groupType.GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
                }
            }
            view.Repaint();
        }
        int finalCount = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        var result = new { captures, failures, originalSelection,
            finalSelection = selected.GetValue(view), originalCount, finalCount,
            viewportActuallyResized = true, state = presenter.Controller.Model.Phase.ToString() };
        if (failures.Count > 0 || finalCount != originalCount)
        {
            Save("native-layout.json", result);
            throw new InvalidOperationException("Native text bounds, preferred heights or GameView restoration failed.");
        }
        return result;
    }

    private static async Task<object> Capture(string state, float scale, List<object> failures)
    {
        Canvas.ForceUpdateCanvases();
        DeathScreenPresenter presenter = Presenter();
        var rows = presenter.GetComponentsInChildren<UnityEngine.UI.Text>(true)
            .Where(Visible).Select(text =>
            {
                Rect box = Box(text.rectTransform);
                bool inBounds = box.xMin >= -.5f && box.yMin >= -.5f &&
                    box.xMax <= Screen.width + .5f && box.yMax <= Screen.height + .5f;
                bool fits = text.preferredHeight <= text.rectTransform.rect.height + 1;
                var row = new { text.name, text.text, text.fontSize, box = new[] { box.x, box.y, box.width, box.height },
                    preferredHeight = text.preferredHeight, actualHeight = text.rectTransform.rect.height, inBounds, fits };
                if (!inBounds || !fits)
                {
                    failures.Add(new { width = Screen.width, height = Screen.height, state, scale, row });
                }
                return row;
            }).ToArray();
        DeathScreenButton[] buttons = presenter.GetComponentsInChildren<DeathScreenButton>(true);
        Rect hint = Box(presenter.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(text => text.name == "Keyboard Hint").rectTransform);
        if (buttons.Any(button => Box((RectTransform)button.transform).Overlaps(hint)))
        {
            failures.Add(new { width = Screen.width, height = Screen.height, state, scale, error = "Hint overlaps action" });
        }
        string path = null;
        if (state == "ready" || Screen.width == 1280 && Screen.height == 720)
        {
            path = Evidence + state + "-" + Screen.width + "x" + Screen.height + "-" +
                (scale > 1 ? "125" : "100") + ".png";
            ScreenCapture.CaptureScreenshot(path);
            await Task.Delay(180);
        }
        return new { state, scale, width = Screen.width, height = Screen.height, rows, path };
    }

    private static bool Visible(UnityEngine.UI.Text text)
    {
        if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text))
        {
            return false;
        }
        foreach (CanvasGroup group in text.GetComponentsInParent<CanvasGroup>())
        {
            if (group.alpha < .01f)
            {
                return false;
            }
        }
        return true;
    }

    private static Rect Box(RectTransform transform)
    {
        var corners = new Vector3[4];
        transform.GetWorldCorners(corners);
        Vector2 minimum = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 maximum = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    private static async Task Ready(DeathScreenPresenter presenter)
    {
        PlayerLife2D player = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single().Player;
        if (presenter.Controller.Model.Phase == DeathScreenPhase.Ready)
        {
            return;
        }
        if (player.IsDead)
        {
            player.RestoreHealth(player.MaximumHealth);
        }
        await Until(() => presenter.Controller.Model.Phase == DeathScreenPhase.Hidden, "Return before next layout state");
        Require(player.TakeDamage(player.MaximumHealth + 1000), "Lethal damage should enter layout case.");
        await Until(() => presenter.Controller.Model.Phase == DeathScreenPhase.Ready, "Layout ready");
    }

    private static async Task Until(Func<bool> condition, string label)
    {
        float deadline = Time.realtimeSinceStartup + 5;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (condition())
            {
                return;
            }
            await Task.Delay(30);
        }
        throw new InvalidOperationException("Timed out: " + label);
    }

    private static DeathScreenPresenter Presenter() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<DeathScreenPresenter>(true)).Single();

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Save(string file, object value) =>
        File.WriteAllText(Evidence + file, JsonConvert.SerializeObject(value, Formatting.Indented));
}
