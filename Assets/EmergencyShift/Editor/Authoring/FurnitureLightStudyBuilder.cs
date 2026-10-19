using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>One-time authoring of the isolated study. Never rewrites an existing scene or shared prefab.</summary>
    public static class FurnitureLightStudyBuilder
    {
        private const string Root = "Assets/EmergencyShift";
        public const string ScenePath = Root + "/Scenes/ArtReview/FurnitureLightStudy.unity";
        private const string ProfilePath = Root + "/Data/ArtReview/FurnitureLightStudy.asset";
        private const string MaterialPath = Root + "/Materials/FurnitureLightStudy.mat";

        [MenuItem("Emergency Shift/Art/Create furniture light study (once)")]
        public static void Create()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Stop Play/compilation. Existing furniture study must be edited in Scene.");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Preserve manual scene edits before opening the furniture study.");
                }
            }

            var profile = CreateProfile();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Materials/FurnitureLightStudy.shader");
                if (shader == null)
                {
                    throw new InvalidOperationException("Furniture study shader was not imported.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var room = new GameObject("Furniture light study · isolated comparison room").transform;
            Floor(room, "Matte tiled floor", Vector2.zero,
                new Vector2(20, 11), "lab-tile-base-01");
            Wall(room, new Vector2(0, 5.5f), new Vector2(20.3f, .3f));
            Wall(room, new Vector2(0, -5.5f), new Vector2(20.3f, .3f));
            Wall(room, new Vector2(-10, 0), new Vector2(.3f, 11));
            Wall(room, new Vector2(10, 0), new Vector2(.3f, 11));

            var rig = Instance(Root + "/Prefabs/Levels/LevelRig.prefab", null, Vector2.zero);
            var player = rig.GetComponentInChildren<PlayerLife2D>();
            var flashlight = player.GetComponent<Flashlight2D>();
            var follow = rig.GetComponentInChildren<CameraFollow2D>();
            player.transform.position = new Vector3(-5, -2.2f, 0);
            follow.Configure(player.transform);
            follow.SnapToTarget();
            var hud = rig.GetComponentInChildren<PrototypeHud>();
            var font = hud.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
            hud.gameObject.SetActive(false);

            foreach (var light in rig.GetComponentsInChildren<Light2D>(true))
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    // Existing masked-multiply style: ambient must keep the sprite's albedo.
                    light.blendStyleIndex = 2;
                    light.color = new Color(.78f, .85f, .9f);
                    light.intensity = .012f;
                }
            }

            var flashlightData = new SerializedObject(flashlight);
            var sources = new[]
            {
                (Light2D)flashlightData.FindProperty("beam").objectReferenceValue,
                (Light2D)flashlightData.FindProperty("nearGlow").objectReferenceValue
            };
            foreach (var light in sources)
            {
                light.blendStyleIndex = 0;
            }

            string[] paths =
            {
                "InteriorProps01/personnel-locker", "IndustrialProps02/lab-workbench", "HospitalService01/transport-stretcher"
            };
            float[] positions = { -5, 0, 5 };
            var samples = new FurnitureLightStudy2D.Sample[paths.Length];
            var ceilings = new Light2D[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                Vector2 position = new Vector2(positions[i], 0);
                var obj = Instance(Root + "/Prefabs/Environment/" + paths[i] + ".prefab", room, position);
                obj.name = profile.surfaces[i].label;
                var box = obj.GetComponent<BoxCollider2D>();
                samples[i] = new FurnitureLightStudy2D.Sample
                {
                    renderer = obj.GetComponent<SpriteRenderer>(),
                    surfaceRect = new Vector4(box.offset.x, box.offset.y, box.size.x * .5f, box.size.y * .5f),
                    responseIndex = i
                };
                ceilings[i] = Ceiling(room, position);
            }

            var controller = room.gameObject.AddComponent<FurnitureLightStudy2D>();
            var status = CreateControls(controller, font);
            controller.Configure(profile, material, flashlight, follow, ceilings, sources, samples, status);
            var events = new GameObject("Study UI events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            RecordOverrides(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save the furniture study.");
            }

            AssetDatabase.SaveAssets();
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var previous = SceneManager.GetSceneAt(i);
                if (previous != scene && !previous.isDirty)
                {
                    EditorSceneManager.CloseScene(previous, true);
                }
            }

            Selection.activeGameObject = room.gameObject;
        }

        private static GameObject Instance(string path, Transform parent, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing furniture study prefab: " + path);
            }

            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            return obj;
        }

        private static void Floor(Transform parent, string name, Vector2 position, Vector2 size, string texture)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Environment/FloorMaterials/" + texture + ".png");
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/FlatLit.mat");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = -10;
        }

        private static void Wall(Transform parent, Vector2 position, Vector2 size)
        {
            var obj = Instance(Root + "/Prefabs/World/Wall.prefab", parent, position);
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            obj.GetComponent<SpriteRenderer>().color = new Color(.08f, .09f, .1f);
        }

        private static void RecordOverrides(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    }
                }
            }
        }

        private static FurnitureLightStudyProfile CreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<FurnitureLightStudyProfile>(ProfilePath);
            if (profile != null)
            {
                profile.Validate();
                return profile;
            }

            if (!AssetDatabase.IsValidFolder(Root + "/Data/ArtReview"))
            {
                AssetDatabase.CreateFolder(Root + "/Data", "ArtReview");
            }

            profile = ScriptableObject.CreateInstance<FurnitureLightStudyProfile>();
            profile.surfaces = new[]
            {
                new FurnitureLightStudyProfile.Response { label = "ШКАФ", topResponse = .12f, edgeResponse = .95f, edgeWidth = .075f },
                new FurnitureLightStudyProfile.Response { label = "СТОЛ", topResponse = .42f, edgeResponse = .95f, edgeWidth = .055f },
                new FurnitureLightStudyProfile.Response { label = "КАТАЛКА", topResponse = .5f, edgeResponse = .9f, edgeWidth = .055f }
            };
            profile.Validate();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }

        private static Light2D Ceiling(Transform parent, Vector2 position)
        {
            var obj = new GameObject("Ceiling comparison light", typeof(Light2D));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 3.5f;
            light.pointLightInnerRadius = 1.4f;
            light.pointLightInnerAngle = 360;
            light.pointLightOuterAngle = 360;
            light.blendStyleIndex = 2;
            light.color = new Color(.85f, .92f, 1);
            light.intensity = .65f;
            light.shadowsEnabled = false;
            light.enabled = false;
            return light;
        }

        private static UnityEngine.UI.Text CreateControls(FurnitureLightStudy2D controller, Font font)
        {
            var canvasObject = new GameObject("Furniture study controls", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var panel = Rect("Comparison panel", canvasObject.transform, new Vector2(20, -18), new Vector2(1500, 150));
            panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.025f, .035f, .04f, .88f);
            string[] names = { "1 · ОБЫЧНЫЙ", "2 · ТЁМНЫЙ ВЕРХ", "3 · КРОМКА" };
            UnityEngine.Events.UnityAction[] actions =
            {
                controller.SelectOriginal, controller.SelectDarkTop, controller.SelectDirectionalEdge
            };
            for (int i = 0; i < names.Length; i++)
            {
                Button(panel, font, names[i], new Vector2(12 + i * 220, -12), new Vector2(210, 46),
                    actions[i]);
            }

            Button(panel, font, "L · ПОТОЛОК", new Vector2(682, -12), new Vector2(174, 46), controller.ToggleCeiling);
            Button(panel, font, "H · ТЕНИ", new Vector2(866, -12), new Vector2(150, 46), controller.ToggleShadows);
            Button(panel, font, "TAB · ПРЕДМЕТ", new Vector2(1026, -12), new Vector2(238, 46), controller.NextSample);
            Button(panel, font, "V · ОБОЙТИ", new Vector2(1274, -12), new Vector2(210, 46), controller.NextSide);
            var status = Label(panel, font, "Status", new Vector2(16, -70), new Vector2(1470, 72), 25);
            status.text = "ПРОБА СВЕТА · нажмите Play";

            var footer = Rect("Movement guide", canvasObject.transform, new Vector2(24, -1030), new Vector2(1810, 40));
            var guide = Label(footer, font, "Controls", Vector2.zero, new Vector2(1810, 40), 24);
            guide.text = "WASD — движение    Мышь — направление    F — фонарь    TAB — другой предмет    V — другая сторона";
            return status;
        }

        private static void Button(Transform parent, Font font, string message, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(message, parent, position, size);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.15f, .21f, .23f, 1);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var label = Label(rect, font, "Label", Vector2.zero, size, 21);
            label.text = message;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private static UnityEngine.UI.Text Label(Transform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = Rect(name, parent, position, size);
            var label = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.color = new Color(.85f, .9f, .88f);
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        [MenuItem("Emergency Shift/Art/Build furniture light study")]
        public static void BuildStudy()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            {
                throw new InvalidOperationException("Stop Play/compilation before building the furniture study.");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save manual scene edits before building the furniture study.");
                }
            }

            Directory.CreateDirectory("Builds/FurnitureLightStudy");
            var target = UnityEditor.Build.NamedBuildTarget.Standalone;
            var previousVariant = PlayerSettings.GetManagedCodeVariant(target);
            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                PlayerSettings.SetManagedCodeVariant(target, ManagedCodeVariant.Instrumented);
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = "Builds/FurnitureLightStudy/EmergencyShift-LightStudy.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development,
                    extraScriptingDefines = new[] { "UNITY_INCLUDE_INSTRUMENTATION" }
                });
            }
            finally
            {
                PlayerSettings.SetManagedCodeVariant(target, previousVariant);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("Furniture light study build: " + report.summary.result + "; errors=" + report.summary.totalErrors);
        }
    }
}
