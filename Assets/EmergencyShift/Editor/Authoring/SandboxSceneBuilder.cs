using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>One-time authoring of editable test rooms; never runs at Play or replaces an existing sandbox.</summary>
    public static class SandboxSceneBuilder
    {
        private const string Root = "Assets/EmergencyShift";
        public const string ScenePath = Root + "/Scenes/MechanicsSandbox.unity";
        private static Scene scene;
        private static Sprite square;
        private static Material lit;
        private static Material unlit;
        private static Font font;

        [MenuItem("Emergency Shift/Sandbox/Create mechanics sandbox (once)")]
        public static void Create()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Stop Play/compilation. Existing sandboxes must be edited in Scene.");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save existing scene edits first.");
                }
            }

            var previous = SceneManager.GetActiveScene();
            square = Load<Sprite>("/Art/Shared/Shapes/Rectangle.asset");
            lit = Load<Material>("/Materials/Shared/FlatLit.mat");
            unlit = Load<Material>("/Materials/Shared/FlatUnlit.mat");
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var rig = Instance("/Prefabs/Levels/LevelRig.prefab", "00 Player · camera · HUD", null, new Vector2(10, -3));
                var player = rig.GetComponentInChildren<PlayerLife2D>();
                player.transform.position = new Vector3(10, -3, 0);
                SetInt(player, "maximumHealth", 3);
                if (player.GetComponent<PlayerAccessCards2D>() == null)
                {
                    player.gameObject.AddComponent<PlayerAccessCards2D>();
                }

                rig.GetComponentInChildren<Camera>().transform.position = new Vector3(10, -3, -10);
                rig.AddComponent<SandboxSession2D>().Configure(player);
                var hud = rig.GetComponentInChildren<PrototypeHud>();
                font = hud.GetComponentInChildren<Text>(true).font;
                var title = Label(hud.transform, "Sandbox controls", new Vector2(24, -48), new Vector2(1100, 65), 16);
                title.text = "ПЕСОЧНИЦА МЕХАНИК  ·  01 СТАЛКЕРЫ  →  02 КАРТА / ДВЕРЬ  →  03 ТИР\n[R] Сброс всех тестов    [T] Пополнить патроны    Вход в комнаты: [E]";
                title.color = new Color(.64f, .8f, .8f);

                var corridor = new GameObject("01 Safe corridor · direct access to every test").transform;
                Shape("Corridor floor", corridor, new Vector2(30, -3), new Vector2(60, 6), new Color(.105f, .14f, .15f));
                CorridorAmbient(corridor);
                Wall("South boundary", corridor, new Vector2(30, -6), new Vector2(60.4f, .4f));
                Wall("West boundary", corridor, new Vector2(0, -3), new Vector2(.4f, 6));
                Wall("East boundary", corridor, new Vector2(60, -3), new Vector2(.4f, 6));
                Shape("Wayfinding stripe", corridor, new Vector2(30, -4.8f), new Vector2(57, .06f), new Color(.22f, .42f, .43f), true);
                Sign(corridor, "СВОБОДНЫЙ ДОСТУП К ТЕСТАМ  →", new Vector2(30, -4), .22f);

                var stalkers = Room(0, "02 Room 01 · Stalkers", new Color(.17f, .19f, .19f));
                Sign(stalkers, "01  ·  СТАЛКЕРЫ", new Vector2(10, 14.5f));
                Sign(stalkers, "Обнаружение · патруль · страх света · укус\nОбычное здоровье: 3 HP", new Vector2(10, 12.9f), .20f);
                Stalker(stalkers, "Stalker · waiting", new Vector2(5, 8.5f), false, null);
                Stalker(stalkers, "Stalker · patrol", new Vector2(14, 9), true, null);
                var cover = Instance("/Prefabs/World/CoverCrate.prefab", "Cover · breaks line of sight", stalkers, new Vector2(9, 7));
                cover.transform.localScale = new Vector3(2, 2, 1);
                Lamp(stalkers, new Vector2(10, 2), 2.5f, .5f);

                var access = Room(20, "03 Room 02 · Access card and locked door", new Color(.18f, .20f, .22f));
                Sign(access, "02  ·  КАРТА И ДВЕРЬ", new Vector2(30, 14.5f));
                Sign(access, "Проверь отказ без карты, затем подбери карту\n[E] Открыть / закрыть · карта не расходуется", new Vector2(30, 12.9f), .20f);
                Wall("Lock test · west partition", access, new Vector2(24.25f, 8.5f), new Vector2(8.5f, .4f));
                Wall("Lock test · east partition", access, new Vector2(35.75f, 8.5f), new Vector2(8.5f, .4f));
                var locked = Door(access, "Locked test door · sandbox-card", new Vector2(30, 8.5f));
                locked.gameObject.AddComponent<DoorAccess2D>().Configure("sandbox-card", "Тестовая карта");
                Instance("/Prefabs/Loot/AccessCardPickup.prefab", "Test access card", access, new Vector2(24, 4))
                    .GetComponent<AccessCardPickup2D>().Configure("sandbox-card", "Тестовая карта");
                Sign(access, "КАРТА ↓", new Vector2(24, 5.2f), .20f);
                Shape("Unlocked side marker", access, new Vector2(30, 10), new Vector2(5, .15f), new Color(.18f, .46f, .31f), true);
                Lamp(access, new Vector2(26, 5), 11, .85f);
                Lamp(access, new Vector2(30, 12), 7, .6f);

                var range = Room(40, "04 Room 03 · Stationary targets and weapons", new Color(.20f, .21f, .22f));
                Sign(range, "03  ·  ОРУЖЕЙНЫЙ ТИР", new Vector2(50, 14.5f));
                Sign(range, "Неподвижные Сталкеры · попадания · кровь / останки\nВнутри: 100 HP · [T] Патроны · [R] Новые цели", new Vector2(50, 12.9f), .20f);
                var healthZone = new GameObject("Range health · 100 HP only inside", typeof(BoxCollider2D), typeof(SandboxHealthZone2D));
                healthZone.transform.SetParent(range, false);
                healthZone.transform.position = new Vector3(50, 8, 0);
                var trigger = healthZone.GetComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(19.5f, 15.5f);
                int[] capacities = { 2, 6, 20 };
                for (int i = 0; i < capacities.Length; i++)
                {
                    int hp = capacities[i];
                    float x = 45 + i * 5;
                    var target = Stalker(range, "Stationary Stalker · " + hp + " HP", new Vector2(x, 9), false, TargetDefinition(hp));
                    target.AttacksEnabled = false;
                    target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    target.gameObject.AddComponent<SandboxStationaryStalker2D>();
                    Sign(range, hp + " HP", new Vector2(x, 10.6f), .20f, 4);
                    Shape("Firing lane", range, new Vector2(x, 6), new Vector2(.04f, 4), new Color(.16f, .28f, .3f), true);
                }

                Shape("Firing line", range, new Vector2(50, 4), new Vector2(17, .10f), new Color(.47f, .38f, .14f), true);
                Instance("/Prefabs/Loot/PistolPickup.prefab", "Range pistol", range, new Vector2(48.5f, 2));
                var ammo = Instance("/Prefabs/Loot/AmmoPickup.prefab", "Range ammunition", range, new Vector2(51.5f, 2));
                ammo.GetComponent<WeaponLootPickup>().Configure(WeaponLootPickup.LootKind.Ammo, 120);
                Lamp(range, new Vector2(46, 7), 12, .9f);
                Lamp(range, new Vector2(54, 7), 12, .9f);
                Sign(corridor, "01  ·  СТАЛКЕРЫ ↑", new Vector2(10, -1.3f), .22f);
                Sign(corridor, "02  ·  КАРТА / ДВЕРЬ ↑", new Vector2(30, -1.3f), .22f);
                Sign(corridor, "03  ·  ТИР / 100 HP ↑", new Vector2(50, -1.3f), .22f);

                ShotgunIntegrationAuthoring.ApplyTo(scene);
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
                    throw new IOException("Could not save sandbox scene.");
                }

                var builds = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
                foreach (var entry in EditorBuildSettings.scenes)
                {
                    if (entry.path != ScenePath && !entry.path.EndsWith("/FirstShift.unity"))
                    {
                        builds.Add(entry);
                    }
                }

                EditorBuildSettings.scenes = builds.ToArray();
                AssetDatabase.SaveAssets();
                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    var old = SceneManager.GetSceneAt(i);
                    if (old != scene)
                    {
                        EditorSceneManager.CloseScene(old, true);
                    }
                }

                Selection.activeGameObject = player.gameObject;
            }
            catch
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
                throw;
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Root + path);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing sandbox dependency: " + path);
            }

            return asset;
        }

        private static GameObject Instance(string path, string name, Transform parent, Vector2 position)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path), scene);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            return obj;
        }

        private static Transform Room(float left, string name, Color floor)
        {
            var room = new GameObject(name).transform;
            float center = left + 10;
            Shape("Floor", room, new Vector2(center, 8), new Vector2(20, 16), floor);
            Wall("North wall", room, new Vector2(center, 16), new Vector2(20, .4f));
            Wall("West wall", room, new Vector2(left, 8), new Vector2(.4f, 16));
            if (left == 40)
            {
                Wall("East wall", room, new Vector2(60, 8), new Vector2(.4f, 16));
            }

            Wall("Entry frame west", room, new Vector2(left + 4.25f, 0), new Vector2(8.5f, .4f));
            Wall("Entry frame east", room, new Vector2(left + 15.75f, 0), new Vector2(8.5f, .4f));
            Door(room, "Entry door · close to isolate test", new Vector2(center, 0));
            return room;
        }

        private static Door2D Door(Transform parent, string name, Vector2 position)
        {
            var obj = Instance("/Prefabs/World/Door.prefab", name, parent, position);
            obj.transform.rotation = Quaternion.Euler(0, 0, 90);
            return obj.GetComponent<Door2D>();
        }

        private static void Wall(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var wall = Instance("/Prefabs/World/Wall.prefab", name, parent, position);
            wall.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = wall.GetComponent<SpriteRenderer>();
            renderer.sharedMaterial = unlit;
            renderer.color = new Color(.05f, .065f, .075f);
        }

        private static void Shape(string name, Transform parent, Vector2 at, Vector2 size, Color tint, bool emissive = false)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.position = at;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.sharedMaterial = emissive ? unlit : lit;
            renderer.color = tint;
            renderer.sortingOrder = emissive ? -9 : -10;
        }

        private static Text Label(Transform parent, string name, Vector2 at, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.anchorMin = new Vector2(0, 1);
            text.rectTransform.anchorMax = new Vector2(0, 1);
            text.rectTransform.pivot = new Vector2(0, 1);
            text.rectTransform.anchoredPosition = at;
            text.rectTransform.sizeDelta = size;
            return text;
        }

        private static void Sign(Transform parent, string message, Vector2 at, float size = .32f, float width = 16)
        {
            var obj = new GameObject("Sign · " + message.Replace('\n', ' '), typeof(RectTransform), typeof(Canvas));
            obj.transform.SetParent(parent, false);
            obj.transform.position = at;
            obj.transform.localScale = Vector3.one * .01f;
            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 6;
            ((RectTransform)obj.transform).sizeDelta = new Vector2(width * 100, 150);
            var text = Label(obj.transform, "Editable room label", Vector2.zero, new Vector2(width * 100, 150), Mathf.RoundToInt(size * 100));
            text.rectTransform.anchorMin = new Vector2(.5f, .5f);
            text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            text.rectTransform.pivot = new Vector2(.5f, .5f);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = message;
            text.color = new Color(.65f, .82f, .82f);
        }

        private static void CorridorAmbient(Transform corridor)
        {
            var obj = new GameObject("Corridor ambient · not defensive light", typeof(Light2D));
            obj.transform.SetParent(corridor, false);
            obj.transform.position = new Vector3(30, -3, 0);
            obj.transform.localScale = new Vector3(60, 6, 1);
            var light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Sprite;
            light.lightCookieSprite = square;
            light.intensity = .35f;
            light.color = new Color(.8f, .88f, 1);
            light.shadowsEnabled = false;
        }

        private static void Lamp(Transform parent, Vector2 position, float radius, float intensity)
        {
            var obj = new GameObject("Test illumination · not defensive light", typeof(Light2D));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var light = obj.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * .45f;
            light.intensity = intensity;
            light.color = new Color(.8f, .88f, 1);
        }

        private static ShadowStalker2D Stalker(Transform parent, string name, Vector2 at, bool patrol, EnemyDefinition definition)
        {
            var actor = Instance("/Prefabs/Enemies/ShadowStalker.prefab", name, parent, at);
            var brain = actor.GetComponent<ShadowStalker2D>();
            if (definition != null)
            {
                var data = new SerializedObject(brain);
                data.FindProperty("definition").objectReferenceValue = definition;
                data.ApplyModifiedPropertiesWithoutUndo();
            }

            var route = new SerializedObject(actor.GetComponent<EnemyPatrol2D>());
            route.FindProperty("patrolEnabled").boolValue = patrol;
            route.FindProperty("usePatrolArea").boolValue = true;
            route.FindProperty("areaSize").vector2Value = new Vector2(4, 4);
            route.ApplyModifiedPropertiesWithoutUndo();
            return brain;
        }

        private static EnemyDefinition TargetDefinition(int hp)
        {
            string path = Root + "/Data/Enemies/SandboxTarget-" + hp + "HP.asset";
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (definition == null)
            {
                definition = UnityEngine.Object.Instantiate(Load<EnemyDefinition>("/Data/Enemies/Stalker-Default.asset"));
                definition.displayName = "Мишень · " + hp + " HP";
                definition.maximumHealth = hp;
                AssetDatabase.CreateAsset(definition, path);
            }

            return definition;
        }

        private static void SetInt(UnityEngine.Object target, string field, int value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
