using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Подготавливает сохранённые пистолет/патроны/эффекты и три профиля без перестройки уровня.</summary>
    public static class WeaponPrototypeSetup
    {
        public const string PistolPath = "Assets/EmergencyShift/Prefabs/PistolPickup.prefab";
        public const string AmmoPath = "Assets/EmergencyShift/Prefabs/AmmoPickup.prefab";
        public const string BulletPath = "Assets/EmergencyShift/Prefabs/PistolBullet.prefab";
        public const string EffectPath = "Assets/EmergencyShift/Prefabs/PistolShotEffect.prefab";
        public const string ProfilesFolder = "Assets/EmergencyShift/Settings/WeaponProfiles";

        [MenuItem("Emergency Shift/Add pistol prototype")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Остановите Play.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath) throw new InvalidOperationException("Откройте BasementPrototype.");
            if (!AssetDatabase.IsValidFolder(ProfilesFolder)) AssetDatabase.CreateFolder("Assets/EmergencyShift/Settings", "WeaponProfiles");
            for (int i = 0; i < 3; i++) EnsureProfile(i);
            EnsureEffect(); EnsureBullet(); EnsureLoot(true); EnsureLoot(false);
            const string playerPath = "Assets/EmergencyShift/Prefabs/Player.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(playerPath);
            try { ConfigurePlayer(contents); PrefabUtility.SaveAsPrefabAsset(contents, playerPath); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            foreach (PlayerInteraction actor in UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsInactive.Include)) ConfigurePlayer(actor.gameObject);
            foreach (PrototypeHud hud in UnityEngine.Object.FindObjectsByType<PrototypeHud>(FindObjectsInactive.Include)) ConfigureHud(hud);
            GameObject group = GameObject.Find("Weapon loot — prototype") ?? new GameObject("Weapon loot — prototype");
            AddLoot(group.transform, "Pistol — ground", PistolPath, new Vector3(1.8f, -1.8f, -0.045f));
            AddLoot(group.transform, "Ammo — Room A", AmmoPath, new Vector3(-1.8f, 1.7f, -0.045f));
            AddLoot(group.transform, "Ammo — Room B", AmmoPath, new Vector3(11.7f, -1.5f, -0.045f));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("PISTOL_PROTOTYPE_READY: E pickup, LMB fire, two ammo boxes, three visual profiles.");
        }

        public static string ProfilePath(int index) => ProfilesFolder + "/" + new[] { "WarmFlash", "ColdTrail", "RedPulse" }[Mathf.Clamp(index, 0, 2)] + ".asset";
        [MenuItem("Emergency Shift/Preview/Shooting/1 Warm flash")] public static void Warm() => SelectProfile(0);
        [MenuItem("Emergency Shift/Preview/Shooting/2 Cold trail")] public static void Cold() => SelectProfile(1);
        [MenuItem("Emergency Shift/Preview/Shooting/3 Red pulse")] public static void Red() => SelectProfile(2);

        private static void SelectProfile(int index)
        {
            var weapon = UnityEngine.Object.FindAnyObjectByType<PlayerWeapon2D>();
            var profile = AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(ProfilePath(index));
            if (weapon == null || profile == null) throw new InvalidOperationException("Сначала подготовьте прототип оружия.");
            weapon.SetVisualProfile(profile);
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(weapon); PrefabUtility.RecordPrefabInstancePropertyModifications(weapon);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        private static void EnsureProfile(int index)
        {
            string path = ProfilePath(index);
            if (AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(path) != null) return;
            var p = ScriptableObject.CreateInstance<ShotVisualProfile>();
            if (index == 1)
            {
                p.displayName = "Холодный след"; p.flashColor = new Color(0.66f, 0.79f, 0.84f);
                p.bulletColor = new Color(0.77f, 0.90f, 0.93f); p.flashSeconds = 0.06f;
                p.flashRadius = 1.5f; p.flashIntensity = 1.7f; p.bulletSpeed = 28;
                p.trailLength = 0.7f; p.trailWidth = 0.02f; p.bulletLightRadius = 0.38f; p.bulletLightIntensity = 0.45f;
            }
            if (index == 2)
            {
                p.displayName = "Красный импульс"; p.flashColor = new Color(1, 0.40f, 0.25f);
                p.bulletColor = new Color(1, 0.64f, 0.38f); p.flashSeconds = 0.12f;
                p.flashRadius = 2.3f; p.flashIntensity = 2.4f; p.bulletSpeed = 24;
                p.trailLength = 0.54f; p.trailWidth = 0.04f; p.bulletLightRadius = 0.3f; p.bulletLightIntensity = 0.3f;
                p.impactSeconds = 0.16f;
            }
            p.name = p.displayName; AssetDatabase.CreateAsset(p, path);
        }

        private static Sprite Shape => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Shapes/Rectangle.asset");
        private static Material Lit => AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
        private static Material Unlit => AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatUnlit.mat");
        private static SpriteRenderer Part(Transform parent, string name, Vector2 position, Vector2 size, Color color, Material material, int order)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(position.x, position.y, -0.001f);
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = obj.GetComponent<SpriteRenderer>(); r.sprite = Shape; r.sharedMaterial = material; r.color = color; r.sortingOrder = order;
            return r;
        }
        private static Light2D Light(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(Light2D)); obj.transform.SetParent(parent, false);
            var light = obj.GetComponent<Light2D>(); light.lightType = Light2D.LightType.Point;
            light.pointLightOuterAngle = 360; light.pointLightInnerAngle = 360;
            light.pointLightOuterRadius = 1; light.intensity = 0; light.shadowIntensity = 1; light.shadowVolumeIntensity = 1;
            var fields = new SerializedObject(light); var layers = fields.FindProperty("m_ApplyToSortingLayers");
            layers.arraySize = 1; layers.GetArrayElementAtIndex(0).intValue = 0; fields.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }
        private static void EnsureEffect()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath) != null) return;
            var root = new GameObject("Pistol shot effect", typeof(ShotEffect2D));
            try
            {
                var strokes = new[] {
                    Part(root.transform, "Forward spark", new Vector2(0, .08f), new Vector2(.035f, .25f), Color.white, Unlit, 2),
                    Part(root.transform, "Cross spark", Vector2.zero, new Vector2(.20f, .035f), Color.white, Unlit, 2),
                    Part(root.transform, "Core", Vector2.zero, new Vector2(.075f, .075f), Color.white, Unlit, 2) };
                root.GetComponent<ShotEffect2D>().Configure(Light(root.transform, "Temporary flash"), strokes);
                PrefabUtility.SaveAsPrefabAsset(root, EffectPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void EnsureBullet()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath) != null) return;
            var root = new GameObject("Pistol bullet", typeof(BulletProjectile2D));
            try
            {
                var trail = Part(root.transform, "Short moving trail", Vector2.zero, new Vector2(.025f, .4f), Color.white, Unlit, 2);
                var tip = Part(root.transform, "Bullet tip", Vector2.zero, new Vector2(.045f, .085f), Color.white, Unlit, 2);
                root.GetComponent<BulletProjectile2D>().ConfigureVisual(trail, tip, Light(root.transform, "Bullet glow"));
                PrefabUtility.SaveAsPrefabAsset(root, BulletPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void GunShape(Transform parent, bool held)
        {
            int order = held ? 2 : 0;
            Vector2 offset = held ? new Vector2(.16f, .22f) : new Vector2(0, -.08f);
            Part(parent, "Grip", offset, new Vector2(.14f, .19f), new Color(.30f, .32f, .33f), Lit, order);
            Part(parent, "Slide", offset + new Vector2(0, .18f), new Vector2(.11f, .34f), new Color(.65f, .68f, .69f), Lit, order);
            Part(parent, "Barrel", offset + new Vector2(0, .37f), new Vector2(.065f, .065f), new Color(.22f, .24f, .25f), Lit, order);
        }
        private static void EnsureLoot(bool pistol)
        {
            string path = pistol ? PistolPath : AmmoPath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject(pistol ? "Pistol pickup" : "Ammo pickup", typeof(CircleCollider2D), typeof(WeaponLootPickup));
            try
            {
                root.transform.position = new Vector3(0, 0, -.045f);
                var trigger = root.GetComponent<CircleCollider2D>(); trigger.radius = .25f; trigger.isTrigger = true;
                root.GetComponent<WeaponLootPickup>().Configure(pistol ? WeaponLootPickup.LootKind.Pistol : WeaponLootPickup.LootKind.Ammo, pistol ? 8 : 12);
                if (pistol) GunShape(root.transform, false);
                else
                {
                    Part(root.transform, "Box", Vector2.zero, new Vector2(.38f, .25f), new Color(.44f, .40f, .32f), Lit, 0);
                    for (int n = 0; n < 3; n++) Part(root.transform, "Cartridge mark " + n, new Vector2(-.09f + n*.09f, 0), new Vector2(.035f, .13f), new Color(.78f, .68f, .43f), Lit, 0);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ConfigurePlayer(GameObject root)
        {
            var weapon = root.GetComponent<PlayerWeapon2D>() ?? root.AddComponent<PlayerWeapon2D>();
            Transform pivot = root.transform.Find("Weapon aim");
            if (pivot == null)
            {
                pivot = new GameObject("Weapon aim").transform; pivot.SetParent(root.transform, false);
                pivot.localPosition = new Vector3(0, 0, -.08f); GunShape(pivot, true);
            }
            Transform muzzle = pivot.Find("Muzzle");
            if (muzzle == null)
            {
                muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false);
                muzzle.localPosition = new Vector3(.16f, .63f, 0);
            }
            weapon.Configure(pivot, muzzle,
                AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath).GetComponent<BulletProjectile2D>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath).GetComponent<ShotEffect2D>(),
                weapon.VisualProfile ?? AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(ProfilePath(0)));
            pivot.gameObject.SetActive(false); EditorUtility.SetDirty(weapon);
            if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.RecordPrefabInstancePropertyModifications(weapon);
        }
        private static void ConfigureHud(PrototypeHud hud)
        {
            Transform existing = hud.transform.Find("Ammo status"); Text text;
            if (existing != null) text = existing.GetComponent<Text>();
            else
            {
                var obj = new GameObject("Ammo status", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                obj.transform.SetParent(hud.transform, false); text = obj.GetComponent<Text>();
                RectTransform rect = text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0); rect.anchoredPosition = new Vector2(-28, 65); rect.sizeDelta = new Vector2(340, 30);
                text.font = hud.transform.Find("Health status").GetComponent<Text>().font;
                text.fontSize = 18; text.alignment = TextAnchor.MiddleRight; text.color = new Color(.78f, .8f, .81f); text.raycastTarget = false;
            }
            text.gameObject.SetActive(false);
            hud.ConfigureWeapon(UnityEngine.Object.FindAnyObjectByType<PlayerWeapon2D>(), text); EditorUtility.SetDirty(hud);
        }
        private static void AddLoot(Transform parent, string name, string path, Vector3 position)
        {
            if (parent.Find(name) != null) return;
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            obj.transform.SetParent(parent, true); obj.name = name; obj.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        }
    }
}
