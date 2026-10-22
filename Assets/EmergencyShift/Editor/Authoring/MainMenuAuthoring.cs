using System;
using System.Collections.Generic;
using EmergencyShift.Features.MainMenu;
using EmergencyShift.Features.MainMenu.Config;
using EmergencyShift.Features.MainMenu.Logic;
using EmergencyShift.Features.MainMenu.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Zenject;

namespace EmergencyShift.Editor
{
    /// <summary>Creates only the new menu; never opens or saves authored gameplay maps.</summary>
    public static class MainMenuAuthoring
    {
        public const string ScenePath = "Assets/EmergencyShift/Scenes/MainMenu.unity";
        public const string PrefabPath = "Assets/EmergencyShift/Prefabs/UI/MainMenu/MainMenuUI.prefab";
        public const string CatalogPath = "Assets/EmergencyShift/Data/UI/MainMenu/SceneCatalog.asset";
        public const string StylePath = "Assets/EmergencyShift/Data/UI/MainMenu/NightShift.asset";
        private const string RowPath = "Assets/EmergencyShift/Prefabs/UI/MainMenu/SceneRow.prefab";
        private const string WorldPath = "Assets/EmergencyShift/Resources/ProjectContext.prefab";
        private static readonly Color Foreground = new Color(0.93f, 0.93f, 0.93f);
        private static readonly Color Muted = new Color(0.62f, 0.62f, 0.62f);

        [MenuItem("Emergency Shift/UI/Create minimal main menu")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Create the menu in Edit Mode.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                throw new InvalidOperationException("MainMenu already exists; edit its prefab instead of regenerating it.");
            }

            EnsureFolder("Assets/EmergencyShift/Data/UI/MainMenu");
            EnsureFolder("Assets/EmergencyShift/Prefabs/UI/MainMenu");
            var catalog = ScriptableObject.CreateInstance<MainMenuCatalog>();
            var catalogData = new SerializedObject(catalog);
            SerializedProperty entries = catalogData.FindProperty("scenes");
            string[] ids = { "sandbox", "morgue", "basement", "template" };
            string[] titles = { "Песочница механик", "Морг — проверка маршрута", "Подвал — прототип", "Шаблон уровня" };
            string[] paths = { "MechanicsSandbox", "MorgueRouteTest", "BasementPrototype", "LevelTemplate" };
            entries.arraySize = ids.Length;
            for (int index = 0; index < ids.Length; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").stringValue = ids[index];
                entry.FindPropertyRelative("title").stringValue = titles[index];
                entry.FindPropertyRelative("scenePath").stringValue = "Assets/EmergencyShift/Scenes/" + paths[index] + ".unity";
            }

            catalogData.ApplyModifiedPropertiesWithoutUndo();
            catalog.ToCatalog();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            var style = ScriptableObject.CreateInstance<MainMenuStyle>();
            style.Validate();
            AssetDatabase.CreateAsset(style, StylePath);
            GameObject row = CreateRow(style);
            GameObject ui = CreateUi(style, row);
            InstallWorld(catalog);
            Scene previous = SceneManager.GetActiveScene();
            Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var composition = new GameObject("Menu composition", typeof(MainMenuContext));
                SceneManager.MoveGameObjectToScene(composition, menu);
                PrefabUtility.InstantiatePrefab(ui, menu);
                var cameraObject = new GameObject("Menu camera", typeof(Camera), typeof(AudioListener));
                SceneManager.MoveGameObjectToScene(cameraObject, menu);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.05f, 0.05f);
                camera.orthographic = true;
                camera.cullingMask = 0;
                var events = new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(events, menu);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                EditorSceneManager.SaveScene(menu, ScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(menu, true);
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }
            }

            var build = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path != ScenePath)
                {
                    build.Add(scene);
                }
            }

            EditorBuildSettings.scenes = build.ToArray();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            MainMenuAudioAuthoring.Apply();
        }

        [MenuItem("Emergency Shift/UI/Use main menu as Play entry")]
        public static void UseMenuEntry() => EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);

        [MenuItem("Emergency Shift/UI/Use current scene as Play entry")]
        public static void UseCurrentEntry() => EditorSceneManager.playModeStartScene = null;

        [MenuItem("Emergency Shift/UI/Apply minimal monochrome style")]
        public static void ApplyMinimalStyle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Edit the menu while Play is stopped.");
            }

            MainMenuStyle style = AssetDatabase.LoadAssetAtPath<MainMenuStyle>(StylePath);
            var styleData = new SerializedObject(style);
            styleData.FindProperty("normalText").colorValue = Muted;
            styleData.FindProperty("selectedText").colorValue = Foreground;
            styleData.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(style);
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform light = root.transform.Find("Flashlight beam");
                if (light != null)
                {
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
                }
                Transform motion = root.transform.Find("Settings/Content/Reduced motion");
                if (motion != null)
                {
                    UnityEngine.Object.DestroyImmediate(motion.gameObject);
                }
                root.GetComponent<UnityEngine.UI.CanvasScaler>().screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                root.transform.Find("Neutral background").GetComponent<UnityEngine.UI.Image>().color = new Color(0.055f, 0.055f, 0.055f);
                foreach (UnityEngine.UI.Text text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/EmergencyShift/Fonts/MainMenu/NotoSans-Medium.ttf");
                    text.color = Muted;
                }
                UnityEngine.UI.Text title = root.transform.Find("Game title").GetComponent<UnityEngine.UI.Text>();
                title.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/EmergencyShift/Fonts/MainMenu/BarlowCondensed-Bold.ttf");
                title.fontStyle = FontStyle.Normal;
                title.fontSize = 60;
                title.lineSpacing = 0.82f;
                title.alignment = TextAnchor.UpperLeft;
                title.horizontalOverflow = HorizontalWrapMode.Overflow;
                title.verticalOverflow = VerticalWrapMode.Overflow;
                title.color = Foreground;
                Place(title.rectTransform, new Vector2(90, -50), new Vector2(400, 150));
                RectTransform navigation = (RectTransform)root.transform.Find("Main actions");
                navigation.anchoredPosition = new Vector2(90, -236);
                string[] buttons = { "New game", "Saves", "Settings", "Quit" };
                float[] offsets = { 0, -57, -114, -197 };
                for (int index = 0; index < buttons.Length; index++)
                {
                    var button = (RectTransform)navigation.Find(buttons[index]);
                    button.anchoredPosition = new Vector2(0, offsets[index]);
                    button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = index == 3 ? 15 : 23;
                }
                var owner = (RectTransform)root.transform.Find("Owner scene catalog");
                owner.anchoredPosition = new Vector2(-90, 24);
                UnityEngine.UI.Text ownerText = owner.GetComponentInChildren<UnityEngine.UI.Text>();
                ownerText.fontSize = 12;
                ownerText.alignment = TextAnchor.MiddleRight;
                foreach (string page in new[] { "NewGame", "Saves", "Settings", "Scenes", "Quit", "Loading" })
                {
                    root.transform.Find(page).GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.08f, 0.08f);
                    root.transform.Find(page + "/Content/Title").GetComponent<UnityEngine.UI.Text>().color = Foreground;
                }
                Transform settings = root.transform.Find("Settings/Content");
                var handle = (RectTransform)settings.Find("Master volume/Handle");
                handle.anchorMin = new Vector2(1, 0.5f);
                handle.anchorMax = new Vector2(1, 0.5f);
                handle.sizeDelta = new Vector2(14, 26);
                handle.GetComponent<UnityEngine.UI.Image>().color = Foreground;
                settings.Find("Master volume/Track").GetComponent<UnityEngine.UI.Image>().color = new Color(0.22f, 0.22f, 0.22f);
                settings.Find("Fullscreen/Box").GetComponent<UnityEngine.UI.Image>().color = new Color(0.22f, 0.22f, 0.22f);
                settings.Find("Fullscreen/Box/Check").GetComponent<UnityEngine.UI.Image>().color = Foreground;
                if (settings.Find("Fullscreen").GetComponent<UnityEngine.UI.Image>() == null)
                {
                    settings.Find("Fullscreen").gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
                }
                ((RectTransform)settings.Find("Apply")).anchoredPosition = new Vector2(0, -350);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            GameObject row = PrefabUtility.LoadPrefabContents(RowPath);
            try
            {
                UnityEngine.UI.Text text = row.GetComponentInChildren<UnityEngine.UI.Text>();
                text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/EmergencyShift/Fonts/MainMenu/NotoSans-Medium.ttf");
                text.fontSize = 23;
                text.color = Muted;
                PrefabUtility.SaveAsPrefabAsset(row, RowPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(row);
            }
            AssetDatabase.DeleteAsset("Assets/EmergencyShift/Materials/UI/MainMenu");
            AssetDatabase.DeleteAsset("Assets/EmergencyShift/Runtime/Features/MainMenu/View/MenuFlashlightView.cs");
            var build = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path != ScenePath)
                {
                    build.Add(scene);
                }
            }
            EditorBuildSettings.scenes = build.ToArray();
            UseMenuEntry();
        }

        private static void InstallWorld(MainMenuCatalog catalog)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(WorldPath);
            try
            {
                if (root.GetComponent<MainMenuWorldInstaller>() != null)
                {
                    throw new InvalidOperationException("World menu composition already exists.");
                }

                var navigation = root.AddComponent<MenuNavigationAdapter>();
                var installer = root.AddComponent<MainMenuWorldInstaller>();
                Assign(installer, "catalog", catalog);
                Assign(installer, "navigation", navigation);
                var context = new SerializedObject(root.GetComponent<ProjectContext>());
                SerializedProperty installers = context.FindProperty("_monoInstallers");
                int index = installers.arraySize;
                installers.arraySize++;
                installers.GetArrayElementAtIndex(index).objectReferenceValue = installer;
                context.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, WorldPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreateRow(MainMenuStyle style)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                RectTransform root = Rect("Scene row", null);
                SceneManager.MoveGameObjectToScene(root.gameObject, preview);
                var layout = root.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.preferredHeight = 62;
                UnityEngine.UI.Button button = Button(root, "Launch scene", "Scene", new Vector2(0, 0), new Vector2(560, 60), style);
                Stretch((RectTransform)button.transform);
                var row = button.gameObject.AddComponent<MenuSceneRowView>();
                Assign(row, "button", button);
                Assign(row, "label", button.GetComponentInChildren<UnityEngine.UI.Text>());
                var rowLayout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                rowLayout.preferredHeight = 62;
                return PrefabUtility.SaveAsPrefabAsset(button.gameObject, RowPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(preview, true);
                SceneManager.SetActiveScene(previous);
            }
        }

        private static GameObject CreateUi(MainMenuStyle style, GameObject row)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                RectTransform root = Rect("Main Menu UI", null);
                SceneManager.MoveGameObjectToScene(root.gameObject, preview);
                var canvas = root.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                var scaler = root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                var view = root.gameObject.AddComponent<MainMenuView>();
                var presenter = root.gameObject.AddComponent<MainMenuPresenter>();
                Assign(presenter, "view", view);
                RectTransform background = Rect("Neutral background", root);
                Stretch(background);
                var fill = background.gameObject.AddComponent<UnityEngine.UI.Image>();
                fill.color = new Color(0.055f, 0.055f, 0.055f);
                fill.raycastTarget = false;
                UnityEngine.UI.Text title = Label(root, "Game title", "EMERGENCY\nSHIFT", new Vector2(90, -50), new Vector2(400, 150), 60, Foreground, FontStyle.Normal);
                title.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/EmergencyShift/Fonts/MainMenu/BarlowCondensed-Bold.ttf");
                title.alignment = TextAnchor.UpperLeft;
                title.lineSpacing = 0.82f;
                title.horizontalOverflow = HorizontalWrapMode.Overflow;
                title.verticalOverflow = VerticalWrapMode.Overflow;
                RectTransform navigation = Rect("Main actions", root);
                Place(navigation, new Vector2(90, -236), new Vector2(330, 300));
                UnityEngine.UI.Button homeNew = PageButton(navigation, "New game", "Новая игра", 0, MenuPage.NewGame, style);
                PageButton(navigation, "Saves", "Сохранения", -57, MenuPage.Saves, style);
                PageButton(navigation, "Settings", "Настройки", -114, MenuPage.Settings, style);
                PageButton(navigation, "Quit", "Выйти из игры", -197, MenuPage.Quit, style);
                UnityEngine.UI.Button owner = PageButton(root, "Owner scene catalog", "Выбор сцены · F8", 0, MenuPage.Scenes, style);
                var ownerRect = (RectTransform)owner.transform;
                ownerRect.anchorMin = Vector2.right;
                ownerRect.anchorMax = Vector2.right;
                ownerRect.pivot = Vector2.right;
                ownerRect.anchoredPosition = new Vector2(-55, 28);
                ownerRect.sizeDelta = new Vector2(260, 42);
                owner.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 12;
                navigation.Find("Quit/Label").GetComponent<UnityEngine.UI.Text>().fontSize = 15;
                Assign(view, "ownerAccess", owner.gameObject);
                var pages = new GameObject[7];
                var first = new UnityEngine.UI.Selectable[7];
                first[0] = homeNew;
                for (int index = 1; index < pages.Length; index++)
                {
                    RectTransform page = Rect(((MenuPage)index).ToString(), root);
                    Place(page, new Vector2(530, -50), new Vector2(680, 570));
                    pages[index] = page.gameObject;
                    var surface = page.gameObject.AddComponent<UnityEngine.UI.Image>();
                    surface.color = new Color(0.08f, 0.08f, 0.08f);
                    surface.raycastTarget = true;
                    RectTransform content = Rect("Content", page);
                    Place(content, new Vector2(34, -24), new Vector2(612, 520));
                    UnityEngine.UI.Button back = PageButton(content, "Back", "← Главное меню", 0, MenuPage.Home, style);
                    back.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 20;
                    first[index] = back;
                    switch ((MenuPage)index)
                    {
                        case MenuPage.NewGame:
                            Title(content, "Новая игра");
                            Label(content, "Start information", "Начать чистую песочницу механик?", new Vector2(0, -125), new Vector2(590, 80), 24, Muted);
                            UnityEngine.UI.Button start = Button(content, "Start", "Начать", new Vector2(0, -245), new Vector2(300, 58), style);
                            Assign(view, "newGame", start);
                            break;
                        case MenuPage.Saves:
                            Title(content, "Сохранения");
                            Label(content, "Empty saves", "Для доступных тестовых сцен\nсохранений пока нет.", new Vector2(0, -140), new Vector2(590, 100), 25, Muted);
                            break;
                        case MenuPage.Settings:
                            CreateSettings(content, view, style);
                            break;
                        case MenuPage.Scenes:
                            Title(content, "Выбор сцены");
                            RectTransform rows = Rect("Catalog rows", content);
                            Place(rows, new Vector2(0, -135), new Vector2(590, 330));
                            var list = rows.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                            list.spacing = 14;
                            list.childControlHeight = true;
                            list.childControlWidth = true;
                            list.childForceExpandHeight = false;
                            Assign(view, "sceneRows", rows);
                            Assign(view, "rowPrefab", row.GetComponent<MenuSceneRowView>());
                            break;
                        case MenuPage.Quit:
                            Title(content, "Выйти из игры?");
                            UnityEngine.UI.Button quit = Button(content, "Confirm exit", "Выйти", new Vector2(0, -160), new Vector2(300, 58), style);
                            Assign(view, "quit", quit);
                            break;
                        case MenuPage.Loading:
                            back.gameObject.SetActive(false);
                            Title(content, "Загрузка…");
                            first[index] = null;
                            break;
                    }

                    page.gameObject.SetActive(false);
                }

                UnityEngine.UI.Text status = Label(root, "Action result", "", new Vector2(530, -640), new Vector2(650, 60), 20, Muted);
                Assign(view, "status", status);
                var serialized = new SerializedObject(view);
                SetArray(serialized.FindProperty("pages"), pages);
                SetArray(serialized.FindProperty("firstSelection"), first);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(preview, true);
                SceneManager.SetActiveScene(previous);
            }
        }

        private static void CreateSettings(RectTransform content, MainMenuView view, MainMenuStyle style)
        {
            Title(content, "Настройки");
            Label(content, "Volume label", "Общая громкость", new Vector2(0, -130), new Vector2(450, 42), 24, Muted);
            UnityEngine.UI.Text value = Label(content, "Volume value", "100%", new Vector2(490, -130), new Vector2(100, 42), 23, Foreground);
            RectTransform sliderRect = Rect("Master volume", content);
            Place(sliderRect, new Vector2(0, -180), new Vector2(580, 34));
            var slider = sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            RectTransform track = Rect("Track", sliderRect);
            Stretch(track);
            track.offsetMin = new Vector2(0, 14);
            track.offsetMax = new Vector2(0, -14);
            track.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.22f, 0.22f, 0.22f);
            RectTransform handle = Rect("Handle", sliderRect);
            handle.anchorMin = new Vector2(0, 0.5f);
            handle.anchorMax = new Vector2(0, 0.5f);
            handle.sizeDelta = new Vector2(14, 26);
            var handleImage = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = Foreground;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;
            Assign(view, "volume", slider);
            Assign(view, "volumeValue", value);
            UnityEngine.UI.Toggle fullscreen = Toggle(content, "Fullscreen", "Полноэкранный режим", -260);
            Assign(view, "fullscreen", fullscreen);
            UnityEngine.UI.Button apply = Button(content, "Apply", "Применить", new Vector2(0, -350), new Vector2(300, 58), style);
            Assign(view, "apply", apply);
        }

        private static UnityEngine.UI.Toggle Toggle(RectTransform parent, string name, string title, float y)
        {
            RectTransform root = Rect(name, parent);
            Place(root, new Vector2(0, y), new Vector2(590, 48));
            root.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
            var toggle = root.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            RectTransform box = Rect("Box", root);
            Place(box, new Vector2(0, -10), new Vector2(28, 28));
            var image = box.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.22f, 0.22f, 0.22f);
            RectTransform check = Rect("Check", box);
            Stretch(check);
            check.offsetMin = new Vector2(6, 6);
            check.offsetMax = new Vector2(-6, -6);
            var mark = check.gameObject.AddComponent<UnityEngine.UI.Image>();
            mark.color = Foreground;
            toggle.targetGraphic = image;
            toggle.graphic = mark;
            Label(root, "Label", title, new Vector2(48, 0), new Vector2(540, 48), 24, Muted);
            return toggle;
        }

        private static void Title(RectTransform parent, string title) => Label(parent, "Title", title, new Vector2(0, -58), new Vector2(600, 60), 38, Foreground);

        private static UnityEngine.UI.Button PageButton(RectTransform parent, string name, string title, float y, MenuPage page, MainMenuStyle style)
        {
            UnityEngine.UI.Button button = Button(parent, name, title, new Vector2(0, y), new Vector2(330, 54), style);
            var link = button.gameObject.AddComponent<MenuPageButton>();
            Assign(link, "button", button);
            var serialized = new SerializedObject(link);
            serialized.FindProperty("page").enumValueIndex = (int)page;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static UnityEngine.UI.Button Button(RectTransform parent, string name, string title, Vector2 position, Vector2 size, MainMenuStyle style)
        {
            RectTransform root = Rect(name, parent);
            Place(root, position, size);
            var hit = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.color = Color.clear;
            var button = root.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = hit;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            UnityEngine.UI.Text label = Label(root, "Label", title, Vector2.zero, size, 23, Muted);
            var highlight = root.gameObject.AddComponent<MenuHighlightTarget>();
            Assign(highlight, "style", style);
            Assign(highlight, "label", label);
            Assign(highlight, "control", button);
            return button;
        }

        private static UnityEngine.UI.Text Label(RectTransform parent, string name, string title, Vector2 position, Vector2 size, int fontSize, Color color, FontStyle fontStyle = FontStyle.Normal)
        {
            RectTransform root = Rect(name, parent);
            Place(root, position, size);
            var text = root.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/EmergencyShift/Fonts/MainMenu/NotoSans-Medium.ttf");
            text.text = title;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }

            return rect;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void Assign(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
