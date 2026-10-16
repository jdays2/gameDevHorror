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
    /// <summary>One-time library authoring; the saved scene owns all later placement edits.</summary>
    public static class EnvironmentLibraryReviewBuilder
    {
        private const string Root = "Assets/EmergencyShift";
        public const string ScenePath = Root + "/Scenes/ArtReview/EnvironmentLibraryReview.unity";

        [Serializable]
        private sealed class Layout
        {
            public Placement[] placements;
        }

        [Serializable]
        private sealed class Placement
        {
            public string family;
            public string id;
            public string label;
            public string collider;
            public float x;
            public float y;
            public float angle;
            public int width;
            public int height;
            public int[] bounds;
            public float pixelsPerUnit;
        }

        [MenuItem("Emergency Shift/Art/Create full environment library scene (once)")]
        public static void Create()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Stop Play/compilation. Existing review scenes must be edited in Scene.");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save manual scene edits before creating a new scene.");
                }
            }

            string project = Directory.GetParent(Application.dataPath).FullName;
            string plan = Path.Combine(project, "Concepts/Used/Environment/EnvironmentLibraryReview/layout.json");
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(plan));
            foreach (var prop in layout.placements)
            {
                Load<Texture2D>(ArtPath(prop));
                if (prop.family != "IndustrialProps02" && File.Exists(PrefabPath(prop)))
                {
                    throw new InvalidOperationException("Existing prefab must be edited explicitly: " + prop.id);
                }
            }

            var material = Load<Material>(Root + "/Materials/FlatLit.mat");
            foreach (var prop in layout.placements)
            {
                if (prop.family == "IndustrialProps02")
                {
                    continue;
                }

                Import(prop);
                CreatePrefab(prop, material);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var room = new GameObject("Library review · warehouse / laboratory / medical and staff").transform;
            var rig = Instance(Root + "/Prefabs/Levels/LevelRig.prefab", null, new Vector2(1, 0));
            var player = rig.GetComponentInChildren<PlayerLife2D>();
            player.transform.position = new Vector3(1, 0, 0);
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
                    text.text = "ОКРУЖЕНИЕ · ОБЗОР АССЕТОВ";
                }
            }

            var warehouse = new GameObject("01 · Warehouse").transform;
            warehouse.SetParent(room, false);
            var laboratory = new GameObject("02 · Laboratory").transform;
            laboratory.SetParent(room, false);
            var medical = new GameObject("03 · Medical and staff").transform;
            medical.SetParent(room, false);
            Floor(warehouse, "Concrete", new Vector2(.25f, 0), new Vector2(10.5f, 10), "warehouse-concrete-base-01");
            Floor(laboratory, "Laboratory tile", new Vector2(11, 0), new Vector2(11, 10), "lab-tile-base-01");
            Floor(medical, "Medical tile", new Vector2(21.75f, 0), new Vector2(10.5f, 10), "lab-tile-base-01");
            Wall(room, new Vector2(11, 5), new Vector2(32.3f, .3f));
            Wall(room, new Vector2(11, -5), new Vector2(32.3f, .3f));
            Wall(room, new Vector2(-5, 0), new Vector2(.3f, 10));
            Wall(room, new Vector2(27, 0), new Vector2(.3f, 10));
            foreach (float x in new[] { 5.5f, 16.5f })
            {
                Wall(room, new Vector2(x, 2.25f), new Vector2(.3f, 5.5f));
                Wall(room, new Vector2(x, -4.15f), new Vector2(.3f, 1.7f));
            }

            foreach (var prop in layout.placements)
            {
                Transform zone = warehouse;
                if (prop.x > 16.5f)
                {
                    zone = medical;
                }
                else if (prop.x > 5.5f)
                {
                    zone = laboratory;
                }

                var obj = Instance(PrefabPath(prop), zone, new Vector2(prop.x, prop.y));
                obj.name = prop.id + " · " + prop.label;
                obj.transform.rotation = Quaternion.Euler(0, 0, prop.angle);
            }

            Sign(warehouse, font, "01 · СКЛАД / ХРАНЕНИЕ", new Vector2(0, -3.65f), 25);
            Sign(warehouse, font, "Поддоны · архив · шкафы · сборная магистраль труб", new Vector2(0, -4.15f), 15);
            Sign(laboratory, font, "02 · ЛАБОРАТОРИЯ / РАБОЧИЕ МЕСТА", new Vector2(11, -3.65f), 25);
            Sign(laboratory, font, "Стекло · микроскопы · документы · ноутбук · компьютер", new Vector2(11, -4.15f), 15);
            Sign(medical, font, "03 · МЕДБЛОК / КОМНАТА ПЕРСОНАЛА", new Vector2(22, -3.65f), 25);
            Sign(medical, font, "Две койки · тележка · диван · ковры · шкафчик", new Vector2(22, -4.15f), 15);
            Ambient(warehouse, new Vector2(.25f, 0), new Vector2(10.5f, 10), new Color(1, .94f, .83f));
            Ambient(laboratory, new Vector2(11, 0), new Vector2(11, 10), new Color(.85f, .94f, 1));
            Ambient(medical, new Vector2(21.75f, 0), new Vector2(10.5f, 10), new Color(.92f, 1, .94f));
            InspectionLight(warehouse, new Vector2(-.9f, 1.8f), new Color(1, .88f, .72f));
            InspectionLight(laboratory, new Vector2(10.25f, 2.7f), new Color(.8f, .92f, 1));
            InspectionLight(medical, new Vector2(21, 1.2f), new Color(.91f, 1, .94f));
            foreach (float x in new[] { 3.6f, 7, 14.7f, 18 })
            {
                var arrow = Instance(Root + "/Prefabs/Environment/FloorMarkings/arrow-forward.prefab", room, new Vector2(x, -2.4f));
                arrow.transform.rotation = Quaternion.Euler(0, 0, -90);
                arrow.GetComponent<SpriteRenderer>().color = new Color(.65f, .74f, .62f, .45f);
            }

            RecordOverrides(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save library review scene.");
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

        private static string ArtPath(Placement prop) => Root + "/Art/Environment/" + prop.family + "/" + prop.id + ".png";
        private static string PrefabPath(Placement prop) => Root + "/Prefabs/Environment/" + prop.family + "/" + prop.id + ".prefab";

        private static void Import(Placement prop)
        {
            EnvironmentSpriteAuthoring.Import(ArtPath(prop), prop.pixelsPerUnit);
        }

        private static void CreatePrefab(Placement prop, Material material)
        {
            string path = PrefabPath(prop);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            var obj = new GameObject(prop.id);
            try
            {
                obj.layer = 8;
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = Load<Sprite>(ArtPath(prop));
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 1;
                if (prop.collider == "none")
                {
                    renderer.sortingOrder = -8;
                }
                else
                {
                    AddCollider(obj, prop);
                    var shadow = obj.AddComponent<ShadowCaster2D>();
                    shadow.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
                    shadow.alphaCutoff = .2f;
                    shadow.trimEdge = 0;
                }

                PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static void AddCollider(GameObject obj, Placement prop)
        {
            if (prop.collider == "outline")
            {
                // Use the imported opaque outline for thin/concave pipes and chair silhouettes.
                var importer = new SerializedObject(AssetImporter.GetAtPath(ArtPath(prop)));
                var outline = importer.FindProperty("m_SpriteSheet.m_Outline");
                var collider = obj.AddComponent<PolygonCollider2D>();
                collider.pathCount = outline.arraySize;
                for (int i = 0; i < outline.arraySize; i++)
                {
                    var points = outline.GetArrayElementAtIndex(i);
                    var path = new Vector2[points.arraySize];
                    for (int j = 0; j < path.Length; j++)
                    {
                        path[j] = points.GetArrayElementAtIndex(j).vector2Value / prop.pixelsPerUnit;
                    }

                    collider.SetPath(i, path);
                }

                return;
            }

            int[] b = prop.bounds;
            var box = obj.AddComponent<BoxCollider2D>();
            box.offset = new Vector2((b[0] + b[2] - prop.width) / (2 * prop.pixelsPerUnit),
                (prop.height - b[1] - b[3]) / (2 * prop.pixelsPerUnit));
            box.size = new Vector2((b[2] - b[0]) / prop.pixelsPerUnit, (b[3] - b[1]) / prop.pixelsPerUnit);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing library dependency: " + path);
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

        private static void Floor(Transform parent, string name, Vector2 position, Vector2 size, string texture)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = Load<Sprite>(Root + "/Art/Environment/FloorMaterials/" + texture + ".png");
            renderer.sharedMaterial = Load<Material>(Root + "/Materials/FlatLit.mat");
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

        private static void Sign(Transform parent, Font font, string message, Vector2 position, int size)
        {
            var obj = new GameObject(message, typeof(RectTransform), typeof(Canvas));
            obj.transform.SetParent(parent, false);
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
            text.color = new Color(.68f, .76f, .72f, .9f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(900, 70);
        }

        private static void Ambient(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject("Review fill · not defensive", typeof(Light2D));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Sprite;
            light.lightCookieSprite = Load<Sprite>(Root + "/Art/Shared/Shapes/Rectangle.asset");
            light.intensity = .38f;
            light.color = color;
            light.shadowsEnabled = false;
        }

        private static void InspectionLight(Transform parent, Vector2 position, Color color)
        {
            var obj = new GameObject("Inspection light · not defensive", typeof(Light2D));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 5.5f;
            light.intensity = .65f;
            light.color = color;
            light.shadowsEnabled = true;
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
    }
}
