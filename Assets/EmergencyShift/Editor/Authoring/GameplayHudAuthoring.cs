using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmergencyShift.Editor
{
    /// <summary>Creates a reusable HUD and adds it beneath the existing Canvas without rebuilding a level.</summary>
    public static class GameplayHudAuthoring
    {
        private const string Root = "Assets/EmergencyShift";
        private const string PrefabPath = Root + "/Prefabs/UI/GameplayHud01.prefab";
        private const string StylePath = Root + "/Data/UI/GameplayHud01.asset";
        private static readonly Color Ink = new Color(.9f, .9f, .86f);
        private static Sprite white;

        [MenuItem("Emergency Shift/UI/Add editable gameplay HUD to current scene")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.isDirty)
            {
                throw new InvalidOperationException("Stop Play and save the current scene first.");
            }

            var old = UnityEngine.Object.FindAnyObjectByType<PrototypeHud>();
            if (old == null || old.gameObject.scene != scene)
            {
                throw new InvalidOperationException("The current scene needs its existing PrototypeHud.");
            }

            var canvas = old.GetComponentInParent<Canvas>();
            if (canvas.GetComponentInChildren<GameplayHudView>(true) != null)
            {
                throw new InvalidOperationException("HUD already installed: edit the Canvas or prefab instead.");
            }

            var font = old.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
            Prepare(font);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas.transform);
            var rect = instance.GetComponent<RectTransform>();
            Stretch(rect);
            var oldData = new SerializedObject(old);
            var life = (PlayerLife2D)oldData.FindProperty("life").objectReferenceValue;
            var weapon = (PlayerWeapon2D)oldData.FindProperty("weapon").objectReferenceValue;
            var flashlight = (Flashlight2D)oldData.FindProperty("flashlight").objectReferenceValue;
            var view = instance.GetComponent<GameplayHudView>();
            var accessCards = life.GetComponent<PlayerAccessCards2D>();
            if (accessCards == null)
            {
                accessCards = Undo.AddComponent<PlayerAccessCards2D>(life.gameObject);
            }

            view.Bind(life, weapon, flashlight, accessCards);
            var pickups = UnityEngine.Object.FindObjectsByType<AccessCardPickup2D>(FindObjectsInactive.Include);
            var serviceCard = Array.Find(pickups, pickup => pickup.CardId == "service");
            if (serviceCard == null)
            {
                serviceCard = Array.Find(pickups, pickup => pickup.CardId == "sandbox-card");
            }

            if (serviceCard != null)
            {
                var viewData = new SerializedObject(view);
                viewData.FindProperty("cardDisplays").GetArrayElementAtIndex(0).FindPropertyRelative("cardId").stringValue = serviceCard.CardId;
                viewData.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            foreach (var field in new[] { "healthStatus", "flashlightStatus", "ammoStatus" })
            {
                var text = (UnityEngine.UI.Text)oldData.FindProperty(field).objectReferenceValue;
                Undo.RecordObject(text, "Replace legacy HUD label");
                text.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }

            foreach (var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (text.gameObject.name == "Controls")
                {
                    Undo.RecordObject(text, "Hide legacy control footer");
                    text.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
        }

        private static void Prepare(Font font)
        {
            Folder(Root + "/Data", "UI");
            Folder(Root + "/Prefabs", "UI");
            foreach (string name in new[] { "pistol", "shotgun", "flashlight-on", "flashlight-off" })
            {
                string path = Root + "/Art/UI/GameplayHud03/" + name + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            white = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/UI/GameplayHud02/white.png");
            var style = AssetDatabase.LoadAssetAtPath<GameplayHudStyle>(StylePath);
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<GameplayHudStyle>();
                AssetDatabase.CreateAsset(style, StylePath);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return;
            }

            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = Rect("Gameplay HUD", null, Vector2.zero, Vector2.zero);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root.gameObject, preview);
                Stretch(root);
                var view = root.gameObject.AddComponent<GameplayHudView>();
                Assign(view, "style", style);
                var left = Rect("Health and access", root, new Vector2(36, 28), new Vector2(270, 110));
                var track = Image("HP bar", left, Vector2.zero, new Vector2(270, 20), new Color(.12f, .17f, .16f));
                var trail = Image("Lost health", track.transform, Vector2.zero, new Vector2(270, 20), style.LostHealth);
                Filled(trail);
                var fill = Image("Current health", track.transform, Vector2.zero, new Vector2(270, 20), style.Healthy);
                Filled(fill);
                var number = Text("HP number - inside bar", track.transform, font, new Vector2(0, 0), new Vector2(262, 20), 14, Ink, "100");
                number.alignment = TextAnchor.MiddleRight;
                var shadow = number.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectDistance = new Vector2(1, -1);
                shadow.effectColor = new Color(0, 0, 0, .8f);
                Assign(view, "healthFill", fill);
                Assign(view, "healthTrail", trail);
                Assign(view, "healthNumber", number);
                var loss = Text("Recent damage", left, font, new Vector2(180, 61), new Vector2(90, 22), 16, style.LostHealth, "");
                loss.alignment = TextAnchor.MiddleRight;
                Assign(view, "lossLabel", loss);
                Assign(view, "condition", Text("Health condition", left, font, new Vector2(0, -21), new Vector2(300, 17), 12, style.Critical, ""));

                var lamp = Rect("Flashlight - above HP", left, new Vector2(0, 30), new Vector2(24, 24));
                Assign(view, "lamp", lamp.gameObject.AddComponent<CanvasGroup>());
                var lampImage = Image("Replaceable flashlight icon", lamp, Vector2.zero, new Vector2(24, 24), Ink);
                lampImage.sprite = Sprite("flashlight-on");
                Assign(view, "lampIcon", lampImage);
                lampImage.preserveAspect = true;
                var slash = Image("Off slash", lamp, Vector2.zero, new Vector2(24, 24), Ink);
                slash.sprite = Sprite("flashlight-off");
                slash.preserveAspect = true;
                Assign(view, "lampSlash", slash.gameObject);
                Text("Flashlight binding", left, font, new Vector2(30, 33), new Vector2(20, 18), 10, Ink, "F");

                var rows = new HudWeaponRow[2];
                var rack = Rect("Weapon positions - edit anchors", root, new Vector2(-296, 28), new Vector2(260, 110));
                rack.anchorMin = rack.anchorMax = Vector2.right;
                var active = Rect("Active weapon position", rack, Vector2.zero, new Vector2(260, 60));
                var spare = Rect("Spare weapon position", rack, new Vector2(0, 57), new Vector2(260, 60));
                Assign(view, "activeWeaponSlot", active);
                Assign(view, "spareWeaponSlot", spare);
                spare.localScale = new Vector3(.78f, .78f, 1);
                string[] definitions = { "PistolDefinition", "PumpShotgun" };
                string[] icons = { "pistol", "shotgun" };
                for (int i = 0; i < rows.Length; i++)
                {
                    var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Root + "/Data/Weapons/" + definitions[i] + ".asset");
                    var row = Rect(definition.DisplayName, i == 0 ? active : spare, Vector2.zero, new Vector2(260, 60));
                    rows[i] = row.gameObject.AddComponent<HudWeaponRow>();
                    Assign(rows[i], "definition", definition);
                    var alpha = row.gameObject.AddComponent<CanvasGroup>();
                    alpha.alpha = i == 0 ? 1 : style.SpareOpacity;
                    Assign(rows[i], "opacity", alpha);
                    var key = Text("Fixed selection key", row, font, new Vector2(0, 25), new Vector2(16, 18), 10, Ink, (definition.SelectionSlot + 1).ToString());
                    key.alignment = TextAnchor.MiddleCenter;
                    var icon = Image("Replaceable weapon icon", row, new Vector2(24, 22), new Vector2(128, 34), Ink);
                    icon.sprite = Sprite(icons[i]);
                    icon.preserveAspect = true;
                    var name = Text("Weapon name", row, font, new Vector2(24, 2), new Vector2(128, 16), 10, Ink, definition.DisplayName.ToUpperInvariant());
                    name.alignment = TextAnchor.MiddleCenter;
                    Assign(rows[i], "ammunition", Text("Active ammunition", row, font, new Vector2(172, 24), new Vector2(40, 32), 28, Ink, "0 / 0"));
                    Assign(rows[i], "reserve", Text("Reserve ammunition", row, font, new Vector2(210, 26), new Vector2(60, 23), 14, Ink, "/ 0"));
                    Assign(rows[i], "operation", Text("Reload or pump", row, font, new Vector2(172, 5), new Vector2(96, 15), 10, Ink, ""));
                }

                var data = new SerializedObject(view);
                var rowArray = data.FindProperty("weaponRows");
                rowArray.arraySize = rows.Length;
                for (int i = 0; i < rows.Length; i++)
                {
                    rowArray.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
                }

                var cardArray = data.FindProperty("cardDisplays");
                cardArray.arraySize = 3;
                string[] ids = { "service", "research", "restricted" };
                string[] cardColors = { "green", "blue", "red" };
                for (int i = 0; i < 3; i++)
                {
                    var card = Rect("Access card - " + ids[i], left, new Vector2(123 + i * 51, 26), new Vector2(45, 28));
                    var body = Image("Card body", card, Vector2.zero, new Vector2(45, 28), Color.white);
                    body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Items/AccessCards01/" + cardColors[i] + ".png");
                    body.preserveAspect = true;
                    var element = cardArray.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("cardId").stringValue = ids[i];
                    element.FindPropertyRelative("visual").objectReferenceValue = card.gameObject;
                }

                data.ApplyModifiedPropertiesWithoutUndo();
                var audioSource = root.gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0;
                Assign(view, "hitAudio", audioSource);
                PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void Folder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/UI/GameplayHud03/" + name + ".png");

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            result.anchorMin = result.anchorMax = result.pivot = Vector2.zero;
            result.anchoredPosition = position;
            result.sizeDelta = size;
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color tint)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = white;
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        private static void Filled(UnityEngine.UI.Image image)
        {
            image.type = UnityEngine.UI.Image.Type.Filled;
            image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
        }

        private static UnityEngine.UI.Text Text(string name, Transform parent, Font font, Vector2 position, Vector2 size, int fontSize, Color tint, string value)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = tint;
            text.text = value;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
