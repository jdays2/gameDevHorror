using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Creates an editable prop review room once; never overwrites existing scenes or prefabs.</summary>
    public static class EnvironmentReviewBuilder
    {
        private const string Root = "Assets/EmergencyShift";
        private const string Art = Root + "/Art/Environment/IndustrialProps02";
        private const string Prefabs = Root + "/Prefabs/Environment/IndustrialProps02";
        public const string ScenePath = Root + "/Scenes/ArtReview/EnvironmentPropsReview.unity";

        [Serializable]
        private sealed class Library
        {
            public Prop[] outputs;
        }

        [Serializable]
        private sealed class Prop
        {
            public string id;
            public string label;
            public int width;
            public int height;
            public int[] solidBoundingBox;
            public float pixelsPerUnit;
        }

        [MenuItem("Emergency Shift/Art/Create environment review room (once)")]
        public static void Create()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Stop Play/compilation. Edit an existing review room in Scene.");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save existing scene edits before opening the review room.");
                }
            }

            string project = Directory.GetParent(Application.dataPath).FullName;
            string sources = Path.Combine(project, "Concepts/Used/Environment/IndustrialProps02");
            var library = JsonUtility.FromJson<Library>(File.ReadAllText(Path.Combine(sources, "provenance.json")));
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Prefabs);
            foreach (var prop in library.outputs)
            {
                string path = Art + "/" + prop.id + ".png";
                if (File.Exists(path) || File.Exists(Prefabs + "/" + prop.id + ".prefab"))
                {
                    throw new InvalidOperationException("Existing art/prefabs must be edited explicitly: " + prop.id);
                }

                File.Copy(Path.Combine(sources, "Sprites", prop.id + ".png"), path);
            }

            AssetDatabase.Refresh();
            var material = Load<Material>(Root + "/Materials/FlatLit.mat");
            var shadowStyle = Load<PropShadowProfile>(Root + "/Data/Environment/LowPropShadows.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach (var prop in library.outputs)
            {
                Import(prop);
                CreatePrefab(prop, material, shadowStyle);
            }

            var room = new GameObject("Environment review · warehouse and laboratory").transform;
            var rig = Instance(Root + "/Prefabs/Levels/LevelRig.prefab", null, Vector2.zero);
            var player = rig.GetComponentInChildren<PlayerLife2D>();
            player.transform.position = new Vector3(0, -1.5f, 0);
            var camera = rig.GetComponentInChildren<Camera>();
            var follow = camera.GetComponent<CameraFollow2D>();
            follow.Configure(player.transform);
            follow.SnapToTarget();
            var hud = rig.GetComponentInChildren<PrototypeHud>();
            var font = hud.GetComponentInChildren<Text>(true).font;
            foreach (var text in hud.GetComponentsInChildren<Text>(true))
            {
                if (text.name == "Title")
                {
                    text.text = "ОКРУЖЕНИЕ · СКЛАД / ЛАБОРАТОРИЯ";
                }
            }

            Floor(room, "Warehouse · concrete", new Vector2(-4, 0), "warehouse-concrete-base-01");
            Floor(room, "Laboratory · tile", new Vector2(4, 0), "lab-tile-base-01");
            Wall(room, new Vector2(0, 5), new Vector2(16.3f, .3f));
            Wall(room, new Vector2(0, -5), new Vector2(16.3f, .3f));
            Wall(room, new Vector2(-8, 0), new Vector2(.3f, 10));
            Wall(room, new Vector2(8, 0), new Vector2(.3f, 10));

            Place(room, "warehouse-steel-shelf", new Vector2(-4.5f, 2.55f), 0);
            Place(room, "warehouse-pallet-crate", new Vector2(-5.1f, -.4f), -4);
            Place(room, "warehouse-drum", new Vector2(-2.2f, 1), 14);
            Place(room, "warehouse-drum", new Vector2(-2.9f, 1.75f), -19);
            Place(room, "lab-workbench", new Vector2(4.5f, 2.5f), 0);
            Place(room, "lab-cabinet", new Vector2(5.7f, -.65f), 0);
            Place(room, "lab-equipment-cabinet", new Vector2(2.25f, .1f), 0);

            Sign(room, font, "СКЛАД · БЕТОН", new Vector2(-4, 3.5f), 30);
            Sign(room, font, "ЛАБОРАТОРИЯ · ПЛИТКА", new Vector2(4, 3.5f), 30);
            Sign(room, font, "01 · ЯЩИК НА ПОДДОНЕ", new Vector2(-5.1f, -1.7f), 17);
            Sign(room, font, "02 · СТЕЛЛАЖ", new Vector2(-4.5f, 1.7f), 17);
            Sign(room, font, "03 · БОЧКИ", new Vector2(-2.5f, .2f), 17);
            Sign(room, font, "04 · РАБОЧИЙ СТОЛ", new Vector2(4.5f, 1.55f), 17);
            Sign(room, font, "05 · ТУМБА", new Vector2(5.7f, -1.4f), 17);
            Sign(room, font, "06 · ОБОРУДОВАНИЕ", new Vector2(2.25f, -.65f), 17);

            Ambient(room, new Vector2(-4, 0), new Color(1, .94f, .84f));
            Ambient(room, new Vector2(4, 0), new Color(.86f, .95f, 1));
            var warehouseLamp = Lamp(room, new Vector2(-4, 3), new Color(1, .9f, .75f));
            var laboratoryLamp = Lamp(room, new Vector2(4, 3), new Color(.8f, .91f, 1));
            foreach (var shadow in room.GetComponentsInChildren<PropGroundShadow2D>())
            {
                shadow.ConfigureLights(player.GetComponent<Flashlight2D>(), new [] { warehouseLamp, laboratoryLamp });
            }
            Marking(room, "arrow-forward", new Vector2(-1, -2.7f), 90, new Color(.8f, .67f, .33f, .7f));
            Marking(room, "arrow-forward", new Vector2(1.4f, -2.7f), -90, new Color(.35f, .65f, .75f, .7f));

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

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save environment review room.");
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

        private static void Import(Prop prop)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/" + prop.id + ".png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = prop.pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = true;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void CreatePrefab(Prop prop, Material material, PropShadowProfile shadowStyle)
        {
            var obj = new GameObject(prop.id);
            try
            {
                obj.layer = 8;
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = Load<Sprite>(Art + "/" + prop.id + ".png");
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 1;
                int[] b = prop.solidBoundingBox;
                var offset = new Vector2((b[0] + b[2] - prop.width) / (2 * prop.pixelsPerUnit),
                    (prop.height - b[1] - b[3]) / (2 * prop.pixelsPerUnit));
                if (prop.id == "warehouse-drum")
                {
                    var circle = obj.AddComponent<CircleCollider2D>();
                    circle.offset = offset;
                    circle.radius = (b[2] - b[0]) / (2 * prop.pixelsPerUnit);
                }
                else
                {
                    var box = obj.AddComponent<BoxCollider2D>();
                    box.offset = offset;
                    box.size = new Vector2((b[2] - b[0]) / prop.pixelsPerUnit, (b[3] - b[1]) / prop.pixelsPerUnit);
                }

                var contact = ShadowRenderer(obj.transform, "Contact shadow", renderer.sprite,
                    Load<Material>(Root + "/Materials/FlatUnlit.mat"), -8);
                contact.color = new Color(0, 0, 0, shadowStyle.contactOpacity);
                var projection = ShadowRenderer(obj.transform, "Bounded light shadow", renderer.sprite,
                    Load<Material>(Root + "/Materials/CreatureSilhouetteShadow.mat"), -7);
                projection.color = new Color(0, 0, 0, 0);
                obj.AddComponent<PropGroundShadow2D>().Configure(shadowStyle, renderer, obj.GetComponent<Collider2D>(), contact, projection);
                PrefabUtility.SaveAsPrefabAsset(obj, Prefabs + "/" + prop.id + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static SpriteRenderer ShadowRenderer(Transform parent, string name, Sprite sprite, Material material, int order)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing review dependency: " + path);
            }

            return asset;
        }

        private static GameObject Instance(string path, Transform parent, Vector2 position)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            return obj;
        }

        private static void Place(Transform room, string id, Vector2 position, float angle)
        {
            var obj = Instance(Prefabs + "/" + id + ".prefab", room, position);
            obj.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private static void Floor(Transform room, string name, Vector2 position, string texture)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(room, false);
            obj.transform.position = position;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Load<Sprite>(Root + "/Art/Environment/FloorMaterials/" + texture + ".png");
            renderer.sharedMaterial = Load<Material>(Root + "/Materials/FlatLit.mat");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(8, 10);
            obj.transform.localScale = Vector3.one;
            renderer.sortingOrder = -10;
        }

        private static void Wall(Transform room, Vector2 position, Vector2 size)
        {
            var obj = Instance(Root + "/Prefabs/World/Wall.prefab", room, position);
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            obj.GetComponent<SpriteRenderer>().color = new Color(.08f, .09f, .1f);
        }

        private static void Sign(Transform room, Font font, string message, Vector2 position, int size)
        {
            var obj = new GameObject(message, typeof(RectTransform), typeof(Canvas));
            obj.transform.SetParent(room, false);
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * .01f;
            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 6;
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(obj.transform, false);
            var text = label.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = message;
            text.color = new Color(.7f, .79f, .76f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(650, 80);
        }

        private static void Ambient(Transform room, Vector2 position, Color color)
        {
            var obj = new GameObject("Local ambient · not defensive", typeof(Light2D));
            obj.transform.SetParent(room, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(8, 10, 1);
            var light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Sprite;
            light.lightCookieSprite = Load<Sprite>(Root + "/Art/Shared/Shapes/Rectangle.asset");
            light.intensity = .2f;
            light.color = color;
            light.shadowsEnabled = false;
        }

        private static Light2D Lamp(Transform room, Vector2 position, Color color)
        {
            var obj = new GameObject("Inspection light · not defensive", typeof(Light2D));
            obj.transform.SetParent(room, false);
            obj.transform.position = position;
            var light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 7;
            light.pointLightInnerRadius = 2;
            light.intensity = .65f;
            light.color = color;
            light.shadowsEnabled = true;
            return light;
        }

        private static void Marking(Transform room, string id, Vector2 position, float angle, Color color)
        {
            var obj = Instance(Root + "/Prefabs/Environment/FloorMarkings/" + id + ".prefab", room, position);
            obj.transform.rotation = Quaternion.Euler(0, 0, angle);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = -9;
        }
    }
}
