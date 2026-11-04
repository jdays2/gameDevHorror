using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.DeathScreen.Config;
using EmergencyShift.Features.DeathScreen.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class InstallDeathScreenD
{
    public const string PrefabPath = "Assets/EmergencyShift/Prefabs/UI/DeathScreenUI.prefab";
    public const string ProfilePath = "Assets/EmergencyShift/Data/UI/DeathScreen/Transition.asset";
    private const string Evidence = "Docs/Validation/DeathScreenD01/";
    private static readonly string[] ScenePaths =
    {
        "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity",
        "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity",
        "Assets/EmergencyShift/Scenes/BasementPrototype.unity",
        "Assets/EmergencyShift/Scenes/LevelTemplate.unity",
        "Assets/EmergencyShift/Scenes/DoorCrushTest.unity"
    };

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Use a stopped, compiled Editor.");
        }
        foreach (string path in ScenePaths)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
            {
                throw new InvalidOperationException("Preserve unsaved scene edits before installing UI: " + path);
            }
        }
        EnsureFolder("Assets/EmergencyShift/Data/UI/DeathScreen");
        DeathScreenProfile profile = AssetDatabase.LoadAssetAtPath<DeathScreenProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<DeathScreenProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        profile.Validate();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            prefab = CreatePrefab(profile);
        }
        Scene active = SceneManager.GetActiveScene();
        var results = new List<object>();
        foreach (string path in ScenePaths)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
            try
            {
                results.Add(InstallScene(scene, prefab));
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
        if (active.IsValid() && active.isLoaded)
        {
            SceneManager.SetActiveScene(active);
        }
        AssetDatabase.SaveAssetIfDirty(profile);
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "installation.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        return results;
    }

    private static object InstallScene(Scene scene, GameObject prefab)
    {
        if (scene.isDirty)
        {
            throw new InvalidOperationException("Do not save unrelated dirty content.");
        }
        Transform[] oldObjects = All<Transform>(scene);
        var before = oldObjects.SelectMany(item => item.GetComponents<Component>())
            .Where(item => item != null).ToDictionary(item => item, Snapshot);
        LevelContext2D context = Unique<LevelContext2D>(scene, true);
        DeathScreenEnvironment environment = Unique<DeathScreenEnvironment>(scene, false);
        bool created = environment == null;
        GameObject root;
        if (created)
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            root.name = "Death Screen";
            environment = root.GetComponent<DeathScreenEnvironment>();
            var gameplay = root.GetComponent<DeathScreenGameplayOverlay>();
            Canvas[] canvases = All<GameplayHudView>(scene).Cast<Component>()
                .Concat(All<PrototypeHud>(scene)).Select(item => item.GetComponentInParent<Canvas>(true))
                .Where(item => item != null).Distinct().ToArray();
            var serialized = new SerializedObject(gameplay);
            SerializedProperty array = serialized.FindProperty("gameplayCanvases");
            array.arraySize = canvases.Length;
            for (int index = 0; index < canvases.Length; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = canvases[index];
            }
            serialized.FindProperty("reticle").objectReferenceValue = Unique<AimReticle2D>(scene, false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(gameplay);
        }
        else
        {
            root = environment.gameObject;
        }
        Assign(context, "deathScreen", environment);
        PrefabUtility.RecordPrefabInstancePropertyModifications(context);
        var changed = before.Where(pair => pair.Value != Snapshot(pair.Key))
            .Select(pair => Hierarchy(pair.Key.transform) + " :: " + pair.Key.GetType().Name).ToArray();
        if (changed.Length != 0)
        {
            throw new InvalidOperationException("Existing authored properties changed: " + string.Join(", ", changed));
        }
        Validate(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("Death UI scene could not be saved: " + scene.path);
        }
        return new { scene = scene.path, created, originalObjects = oldObjects.Length,
            unchangedOriginalComponents = before.Count, added = Hierarchy(root.transform),
            prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root),
            checkpointsAdded = 0, audioSourcesAdded = root.GetComponentsInChildren<AudioSource>(true).Length };
    }

    public static object Review() => ReviewTo(Evidence);

    public static object ReviewD02() => ReviewTo("Docs/Validation/DeathScreenD02/");

    private static object ReviewTo(string evidencePath)
    {
        var results = new List<object>();
        Scene active = SceneManager.GetActiveScene();
        string originalPath = active.path;
        if (SceneManager.sceneCount != 1 || !active.isLoaded || active.isDirty ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Review requires one clean stopped scene; preserve unsaved work first.");
        }
        foreach (string path in ScenePaths)
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                throw new InvalidOperationException("Review must not discard a dirty scene.");
            }
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            results.Add(Validate(scene));
        }
        if (SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Review must not discard a dirty scene.");
        }
        EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        Directory.CreateDirectory(evidencePath);
        File.WriteAllText(evidencePath + "authoring-review.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        return results;
    }

    private static object Validate(Scene scene)
    {
        Transform[] objects = All<Transform>(scene);
        DeathScreenEnvironment environment = Unique<DeathScreenEnvironment>(scene, true);
        DeathScreenPresenter presenter = Unique<DeathScreenPresenter>(scene, true);
        DeathScreenView view = Unique<DeathScreenView>(scene, true);
        LevelContext2D context = Unique<LevelContext2D>(scene, true);
        SerializedProperty link = new SerializedObject(context).FindProperty("deathScreen");
        if (link.objectReferenceValue != environment || environment.Profile == null ||
            environment.GetComponent<DeathScreenGameplayOverlay>() == null ||
            !PrefabUtility.IsPartOfPrefabInstance(environment))
        {
            throw new InvalidOperationException("Invalid UI ownership, context link or profile in " + scene.path);
        }
        var viewData = new SerializedObject(view);
        foreach (string field in new[] { "overlay", "shade", "iris", "blackout", "content", "loading", "title",
            "message", "primaryLabel", "secondaryLabel", "hint", "primary", "secondary", "gameplay", "fallbackEvents" })
        {
            if (viewData.FindProperty(field).objectReferenceValue == null)
            {
                throw new InvalidOperationException("Missing death screen reference: " + field);
            }
        }
        environment.Profile.Validate();
        DeathScreenIris iris = Unique<DeathScreenIris>(scene, true);
        if (viewData.FindProperty("iris").objectReferenceValue != iris ||
            iris.gameObject != ((CanvasGroup)viewData.FindProperty("shade").objectReferenceValue).gameObject)
        {
            throw new InvalidOperationException("The black iris must belong to this screen's world veil.");
        }
        int missingScripts = objects.Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
        PersistentEntity2D[] identities = All<PersistentEntity2D>(scene);
        if (missingScripts != 0 || identities.Any(item => string.IsNullOrWhiteSpace(item.Id)) ||
            identities.Select(item => item.Id).Distinct().Count() != identities.Length)
        {
            throw new InvalidOperationException("Missing scripts or invalid existing persistent identity in " + scene.path);
        }
        var root = environment.gameObject;
        if (root.GetComponentsInChildren<AudioSource>(true).Length != 0 ||
            root.GetComponentsInChildren<Camera>(true).Length != 0 ||
            root.GetComponentsInChildren<AudioListener>(true).Length != 0)
        {
            throw new InvalidOperationException("Death UI must not add sound or cameras.");
        }
        int activeEvents = All<EventSystem>(scene).Count(item => item.isActiveAndEnabled);
        if (activeEvents > 1)
        {
            throw new InvalidOperationException("Multiple active EventSystems in " + scene.path);
        }
        return new { scene = scene.path, scene.isDirty, root = Hierarchy(root.transform),
            position = new[] { root.transform.localPosition.x, root.transform.localPosition.y, root.transform.localPosition.z },
            rotation = new[] { root.transform.localRotation.x, root.transform.localRotation.y, root.transform.localRotation.z, root.transform.localRotation.w },
            scale = new[] { root.transform.localScale.x, root.transform.localScale.y, root.transform.localScale.z },
            prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root), missingScripts,
            persistentIds = identities.Length, activeEvents, cameras = All<Camera>(scene).Length,
            listeners = All<AudioListener>(scene).Length, contexts = All<LevelContext2D>(scene).Length,
            deathScreens = All<DeathScreenPresenter>(scene).Length,
            audioSourcesInDeathScreen = root.GetComponentsInChildren<AudioSource>(true).Length,
            closureSeconds = environment.Profile.ToSettings().WorldFadeSeconds,
            worldPauseDelaySeconds = environment.Profile.ToSettings().WorldPauseDelaySeconds,
            savedRuntimeWorldAudio = All<EmergencyShift.Features.WorldAudio.View.WorldAudioPresenter>(scene).Length,
            startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) };
    }

    private static GameObject CreatePrefab(DeathScreenProfile profile)
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/EmergencyShift/Fonts/MainMenu/NotoSans-Medium.ttf");
        if (font == null)
        {
            throw new InvalidOperationException("The accepted menu font is missing.");
        }
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Death Screen");
            SceneManager.MoveGameObjectToScene(root, preview);
            var environment = root.AddComponent<DeathScreenEnvironment>();
            var presenter = root.AddComponent<DeathScreenPresenter>();
            var gameplay = root.AddComponent<DeathScreenGameplayOverlay>();
            Assign(environment, "profile", profile);
            RectTransform canvasRect = Rect("Death Canvas", root.transform);
            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvas.enabled = false;
            var scaler = canvasRect.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            canvasRect.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var view = canvasRect.gameObject.AddComponent<DeathScreenView>();
            CanvasGroup shade = Layer("World Veil", canvasRect, Color.black);
            Object.DestroyImmediate(shade.GetComponent<UnityEngine.UI.Image>());
            DeathScreenIris iris = shade.gameObject.AddComponent<DeathScreenIris>();
            iris.color = Color.black;
            iris.raycastTarget = false;
            CanvasGroup blackout = Layer("Transition Black", canvasRect, Color.black);
            blackout.alpha = 0;
            RectTransform contentRect = Rect("Message and Actions", canvasRect);
            Stretch(contentRect);
            CanvasGroup content = contentRect.gameObject.AddComponent<CanvasGroup>();
            RectTransform column = Rect("Menu Column", contentRect);
            column.anchorMin = new Vector2(.5f, 1);
            column.anchorMax = new Vector2(.5f, 1);
            column.pivot = new Vector2(0, 1);
            column.anchoredPosition = new Vector2(-448, -180);
            column.sizeDelta = new Vector2(520, 460);
            Layout(column, 0);
            Label("Game Title", column, font, "АВАРИЙНАЯ СМЕНА", 16, new Color(.65f, .72f, .71f), 520);
            Spacer(column, 20);
            var title = Label("Death Title", column, font, "ВЫ ПОГИБЛИ", 46, new Color(.93f, .93f, .92f), 520);
            Spacer(column, 12);
            RectTransform rule = Rect("Accent Rule", column);
            Element(rule, 36, 2);
            rule.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.62f, .19f, .17f);
            rule.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Spacer(column, 24);
            var message = Label("Return Context", column, font, "С начала сцены", 18, new Color(.68f, .72f, .71f), 520);
            Spacer(column, 18);
            RectTransform actions = Rect("Actions", column);
            Element(actions, 400, 128);
            Layout(actions, 12);
            DeathScreenButton primary = Button("Retry or Stay", actions, font, "Повторить попытку", out var primaryLabel);
            DeathScreenButton secondary = Button("Quit or Confirm", actions, font, "Выйти из игры", out var secondaryLabel);
            primary.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = secondary, selectOnDown = secondary };
            secondary.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = primary, selectOnDown = primary };
            var hint = Label("Keyboard Hint", contentRect, font, "↑ ↓ Выбор   Enter Подтвердить   R Повторить", 16, new Color(.68f, .72f, .71f), 760);
            hint.rectTransform.anchorMin = new Vector2(.5f, 0);
            hint.rectTransform.anchorMax = new Vector2(.5f, 0);
            hint.rectTransform.pivot = Vector2.zero;
            hint.rectTransform.anchoredPosition = new Vector2(-448, 106);
            hint.rectTransform.sizeDelta = new Vector2(760, 34);
            RectTransform loadingRect = Rect("Loading Status", canvasRect);
            loadingRect.anchorMin = new Vector2(.5f, 1);
            loadingRect.anchorMax = new Vector2(.5f, 1);
            loadingRect.pivot = new Vector2(0, 1);
            loadingRect.anchoredPosition = new Vector2(-430, -372);
            loadingRect.sizeDelta = new Vector2(520, 58);
            CanvasGroup loading = loadingRect.gameObject.AddComponent<CanvasGroup>();
            loading.alpha = 0;
            loading.blocksRaycasts = false;
            var loadingText = loadingRect.gameObject.AddComponent<UnityEngine.UI.Text>();
            Text(loadingText, font, "Загрузка…", 24, new Color(.9f, .93f, .92f));
            var events = new GameObject("UI EventSystem - fallback", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(root.transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            events.SetActive(false);
            foreach (var pair in new Dictionary<string, Object> { { "overlay", canvas }, { "shade", shade }, { "iris", iris },
                { "blackout", blackout }, { "content", content }, { "loading", loading }, { "title", title },
                { "message", message }, { "primaryLabel", primaryLabel }, { "secondaryLabel", secondaryLabel },
                { "hint", hint }, { "primary", primary }, { "secondary", secondary }, { "gameplay", gameplay },
                { "fallbackEvents", events } })
            {
                Assign(view, pair.Key, pair.Value);
            }
            Assign(presenter, "view", view);
            GameObject result = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved || result == null)
            {
                throw new InvalidOperationException("Death screen prefab could not be saved.");
            }
            return result;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static DeathScreenButton Button(string name, Transform parent, Font font, string text,
        out UnityEngine.UI.Text label)
    {
        RectTransform rect = Rect(name, parent);
        Element(rect, 400, 58);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        var button = rect.gameObject.AddComponent<DeathScreenButton>();
        button.targetGraphic = image;
        button.colors = new UnityEngine.UI.ColorBlock
        {
            normalColor = new Color(18 / 255f, 23 / 255f, 23 / 255f, .75f),
            highlightedColor = new Color(41 / 255f, 53 / 255f, 53 / 255f, 1),
            selectedColor = new Color(41 / 255f, 53 / 255f, 53 / 255f, 1),
            pressedColor = new Color(.2f, .26f, .26f, 1),
            disabledColor = new Color(.055f, .07f, .07f, .65f),
            colorMultiplier = 1,
            fadeDuration = .08f
        };
        RectTransform labelRect = Rect("Label", rect);
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(18, 4);
        labelRect.offsetMax = new Vector2(-18, -4);
        label = labelRect.gameObject.AddComponent<UnityEngine.UI.Text>();
        Text(label, font, text, 24, new Color(.9f, .93f, .92f));
        label.alignment = TextAnchor.MiddleLeft;
        return button;
    }

    private static UnityEngine.UI.Text Label(string name, Transform parent, Font font, string text, int size, Color color, float width)
    {
        RectTransform rect = Rect(name, parent);
        Element(rect, width, -1);
        var label = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        Text(label, font, text, size, color);
        return label;
    }

    private static void Text(UnityEngine.UI.Text label, Font font, string text, int size, Color color)
    {
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        label.supportRichText = false;
    }

    private static CanvasGroup Layer(string name, Transform parent, Color color)
    {
        RectTransform rect = Rect(name, parent);
        Stretch(rect);
        rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = color;
        return rect.gameObject.AddComponent<CanvasGroup>();
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        result.SetParent(parent, false);
        result.pivot = new Vector2(0, 1);
        return result;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Element(RectTransform rect, float width, float height)
    {
        var element = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
    }

    private static void Spacer(Transform parent, float height) => Element(Rect("Spacing", parent), 0, height);

    private static void Layout(RectTransform rect, float spacing)
    {
        var layout = rect.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void Assign(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            throw new InvalidOperationException("Missing authored field: " + field);
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T[] All<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static T Unique<T>(Scene scene, bool required) where T : Component
    {
        T[] values = All<T>(scene);
        if (values.Length > 1 || required && values.Length != 1)
        {
            throw new InvalidOperationException("Choose one " + typeof(T).Name + " in " + scene.path);
        }
        return values.FirstOrDefault();
    }

    private static string Snapshot(Component item)
    {
        JObject json = JObject.Parse(EditorJsonUtility.ToJson(item));
        if (item is LevelContext2D)
        {
            json.Remove("deathScreen");
        }
        return json.ToString(Formatting.None);
    }

    private static string Hierarchy(Transform value) =>
        value.parent == null ? value.name : Hierarchy(value.parent) + "/" + value.name;

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
