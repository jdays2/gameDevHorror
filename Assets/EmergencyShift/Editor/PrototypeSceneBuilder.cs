using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Однократно создаёт и сохраняет редактируемую сцену. В игре не выполняется.</summary>
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/EmergencyShift/Scenes/BasementPrototype.unity";
        private const string Root = "Assets/EmergencyShift";
        private static Sprite rectangle;
        private static Sprite capsule;
        private static Material lit;
        private static Material unlit;

        [MenuItem("Emergency Shift/Create prototype scene (once)")]
        public static void Build()
        {
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("Сцена уже существует. Ручные правки не будут перезаписаны.");
            foreach (string folder in new[] { "Scenes", "Shapes", "Materials", "Prefabs", "Settings" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            rectangle = CreateShape("Rectangle", 32, 32, false);
            capsule = CreateShape("Capsule", 32, 48, true);
            lit = CreateMaterial("FlatLit", "Universal Render Pipeline/2D/Sprite-Lit-Default");
            unlit = CreateMaterial("FlatUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            ConfigureRendering();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = new GameObject("Level — editable geometry").transform;
            var floors = new GameObject("Floors").transform;
            floors.SetParent(level);
            var walls = new GameObject("Walls").transform;
            walls.SetParent(level);

            Color floorColor = new Color(0.22f, 0.25f, 0.27f);
            Color wallColor = new Color(0.42f, 0.44f, 0.46f);
            // Две комнаты и соединяющий их короткий коридор. Всё — объекты сцены.
            Solid("Room A floor", floors, new Vector2(-2, 0), new Vector2(12, 8), floorColor, false);
            Solid("Corridor floor", floors, new Vector2(7, 0), new Vector2(6, 3), floorColor, false);
            Solid("Room B floor", floors, new Vector2(13.5f, 0), new Vector2(7, 8), floorColor, false);
            var wallTemplate = Solid("Wall template", null, Vector2.zero, Vector2.one, wallColor, true);
            string wallPrefab = Root + "/Prefabs/Wall.prefab";
            PrefabUtility.SaveAsPrefabAsset(wallTemplate, wallPrefab);
            UnityEngine.Object.DestroyImmediate(wallTemplate);

            Wall("A north", walls, -2, 4.15f, 12.6f, 0.3f);
            Wall("A south", walls, -2, -4.15f, 12.6f, 0.3f);
            Wall("A west", walls, -8.15f, 0, 0.3f, 8);
            Wall("A east upper", walls, 4.15f, 2.75f, 0.3f, 2.5f);
            Wall("A east lower", walls, 4.15f, -2.75f, 0.3f, 2.5f);
            Wall("Corridor north", walls, 7.15f, 1.65f, 5.7f, 0.3f);
            Wall("Corridor south", walls, 7.15f, -1.65f, 5.7f, 0.3f);
            Wall("B west upper", walls, 9.85f, 2.75f, 0.3f, 2.5f);
            Wall("B west lower", walls, 9.85f, -2.75f, 0.3f, 2.5f);
            Wall("B north", walls, 13.5f, 4.15f, 7.6f, 0.3f);
            Wall("B south", walls, 13.5f, -4.15f, 7.6f, 0.3f);
            Wall("B east", walls, 17.15f, 0, 0.3f, 8);

            var doorObject = new GameObject("Door — E to open");
            doorObject.transform.SetParent(level);
            doorObject.transform.position = new Vector3(4.15f, 0, 0);
            var leaf = Solid("Leaf", doorObject.transform, Vector2.zero, new Vector2(0.3f, 3), new Color(0.48f, 0.47f, 0.43f), true);
            leaf.transform.localPosition = Vector3.zero;
            Door2D door = doorObject.AddComponent<Door2D>();
            door.Configure(leaf.transform);
            PrefabUtility.SaveAsPrefabAssetAndConnect(doorObject, Root + "/Prefabs/Door.prefab", InteractionMode.AutomatedAction);

            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50;
            cameraObject.transform.position = new Vector3(2.8f, 0, -10);
            cameraObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);

            var player = new GameObject("Player — capsule");
            player.transform.position = new Vector3(2.8f, 0, 0);
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.56f, 0.88f);
            var physicalMaterial = new PhysicsMaterial2D("No friction") { friction = 0, bounciness = 0 };
            AssetDatabase.CreateAsset(physicalMaterial, Root + "/Materials/NoFriction.physicsMaterial2D");
            collider.sharedMaterial = physicalMaterial;
            player.AddComponent<PlayerMotor2D>();

            var visual = new GameObject("Capsule visual", typeof(SpriteRenderer));
            visual.transform.SetParent(player.transform, false);
            var playerRenderer = visual.GetComponent<SpriteRenderer>();
            playerRenderer.sprite = capsule;
            playerRenderer.sharedMaterial = unlit;
            playerRenderer.color = new Color(0.60f, 0.62f, 0.62f);
            playerRenderer.sortingOrder = 10;
            visual.transform.localScale = Vector3.one * 0.88f;

            var aim = new GameObject("Flashlight aim").transform;
            aim.SetParent(player.transform, false);
            aim.localRotation = Quaternion.Euler(0, 0, -90);
            var lampObject = new GameObject("Flashlight beam", typeof(Light2D));
            lampObject.transform.SetParent(aim, false);
            // Источник остаётся внутри коллайдера капсулы даже у боковой стены.
            lampObject.transform.localPosition = new Vector3(0, 0.18f, 0);
            Light2D beam = lampObject.GetComponent<Light2D>();
            beam.lightType = Light2D.LightType.Point;
            beam.color = new Color(0.83f, 0.89f, 1);
            beam.intensity = 1.5f;
            beam.pointLightInnerRadius = 0.35f;
            beam.pointLightOuterRadius = 4.2f;
            beam.pointLightInnerAngle = 48;
            beam.pointLightOuterAngle = 80;
            beam.shadowIntensity = 1;
            beam.shadowsEnabled = true;
            beam.enabled = false;
            var glowObject = new GameObject("Flashlight spill — close range", typeof(Light2D));
            glowObject.transform.SetParent(player.transform, false);
            Light2D nearGlow = glowObject.GetComponent<Light2D>();
            nearGlow.lightType = Light2D.LightType.Point;
            nearGlow.color = beam.color;
            nearGlow.intensity = 0.18f;
            nearGlow.pointLightInnerRadius = 0;
            nearGlow.pointLightOuterRadius = 0.65f;
            nearGlow.pointLightInnerAngle = nearGlow.pointLightOuterAngle = 360;
            nearGlow.shadowIntensity = 1;
            nearGlow.shadowsEnabled = true;
            nearGlow.enabled = false;
            Flashlight2D flashlight = player.AddComponent<Flashlight2D>();
            flashlight.Configure(camera, aim, beam, nearGlow);
            PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
            interaction.Configure(new[] { door });
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, Root + "/Prefabs/Player.prefab", InteractionMode.AutomatedAction);
            // Ссылки на камеру и дверь — переопределения экземпляра в этой сцене.
            flashlight.Configure(camera, aim, beam, nearGlow);
            interaction.Configure(new[] { door });
            PrefabUtility.RecordPrefabInstancePropertyModifications(flashlight);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            cameraObject.AddComponent<CameraFollow2D>().Configure(player.transform);

            var ambient = new GameObject("Darkness — ambient 0.006", typeof(Light2D));
            Light2D ambientLight = ambient.GetComponent<Light2D>();
            ambientLight.lightType = Light2D.LightType.Global;
            ambientLight.color = new Color(0.6f, 0.7f, 0.8f);
            ambientLight.intensity = 0.006f;
            CreateHud(flashlight, interaction);

            PlayerSettings.companyName = "EmergencyShift";
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            PlayerSettings.productName = "Emergency Shift";
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(3.5f, 0), new Vector3(27, 12, 1)), false);
            Debug.Log("EMERGENCY_SHIFT_SCENE_READY: " + ScenePath);
        }

        private static Sprite CreateShape(string name, int width, int height, bool rounded)
        {
            string path = Root + "/Shapes/" + name + ".asset";
            if (File.Exists(path)) return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear };
            var colors = new Color[width * height];
            float radius = width / 2f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float centerY = Mathf.Clamp(y + 0.5f, radius, height - radius);
                    bool inside = !rounded || Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, centerY)) <= radius;
                    colors[y * width + x] = inside ? Color.white : Color.clear;
                }
            texture.SetPixels(colors);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), rounded ? height : width);
            sprite.name = name;
            AssetDatabase.AddObjectToAsset(sprite, texture);
            return sprite;
        }

        private static Material CreateMaterial(string name, string shaderName)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find(shaderName)) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureRendering()
        {
            string rendererPath = Root + "/Settings/Prototype2DRenderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            string pipelinePath = Root + "/Settings/PrototypeURP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            for (int quality = 0; quality < QualitySettings.names.Length; quality++)
            {
                QualitySettings.SetQualityLevel(quality, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(0, false);
            var rendererSettings = new SerializedObject(renderer);
            rendererSettings.FindProperty("m_LightRenderTextureScale").floatValue = 1;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Solid(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool blocks)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = rectangle;
            renderer.sharedMaterial = lit;
            renderer.color = color;
            renderer.sortingOrder = blocks ? 2 : 0;
            if (blocks)
            {
                obj.AddComponent<BoxCollider2D>();
                var caster = obj.AddComponent<ShadowCaster2D>();
                caster.enabled = false;
                var serialized = new SerializedObject(caster);
                // Shape Editor в URP 17.6: явный квадрат в локальных координатах.
                serialized.FindProperty("m_ShadowCastingSource").intValue = 1;
                var path = serialized.FindProperty("m_ShapePath");
                path.arraySize = 4;
                var vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f), new Vector3(0.5f, -0.5f) };
                for (int i = 0; i < 4; i++) path.GetArrayElementAtIndex(i).vector3Value = vertices[i];
                serialized.FindProperty("m_ShapePathHash").intValue = 12345;
                serialized.FindProperty("m_CastingOption").intValue = 2;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                caster.enabled = true;
            }
            return obj;
        }

        private static void Wall(string name, Transform parent, float x, float y, float width, float height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Wall.prefab");
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.name = name;
            obj.transform.position = new Vector3(x, y, 0);
            obj.transform.localScale = new Vector3(width, height, 1);
        }

        private static void CreateHud(Flashlight2D flashlight, PlayerInteraction interaction)
        {
            var canvasObject = new GameObject("HUD — minimal", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            Text title = Label("Title", canvas.transform, "АВАРИЙНАЯ СМЕНА", new Vector2(0, 1), new Vector2(28, -27), new Vector2(420, 32), 19);
            title.color = new Color(0.55f, 0.58f, 0.59f);
            Text status = Label("Flashlight status", canvas.transform, "[F]  ВКЛЮЧИТЬ ФОНАРЬ", Vector2.zero, new Vector2(28, 55), new Vector2(420, 30), 18);
            Label("Controls", canvas.transform, "WASD — движение    Мышь — направление света", Vector2.zero, new Vector2(28, 27), new Vector2(750, 28), 15).color = new Color(0.45f, 0.49f, 0.51f);
            Text prompt = Label("Door prompt", canvas.transform, "Нажмите «E», чтобы открыть", new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(520, 40), 20);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            prompt.gameObject.SetActive(false);
            canvasObject.AddComponent<PrototypeHud>().Configure(flashlight, interaction, status, prompt);
        }

        private static Text Label(string name, Transform parent, string content, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = obj.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = new Color(0.78f, 0.80f, 0.81f);
            text.raycastTarget = false;
            text.alignment = anchor.y > 0.5f ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            return text;
        }
    }
}
