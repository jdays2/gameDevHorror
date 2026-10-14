using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Импортирует кадры и добавляет один редактируемый экземпляр врага, сохраняя планировку.</summary>
    [InitializeOnLoad]
    public static class StalkerSceneSetup
    {
        private const string PendingKey = "EmergencyShift.StalkerSetupPending";
        private const string Art = "Assets/EmergencyShift/Art/Stalker";
        private const string PrefabPath = "Assets/EmergencyShift/Prefabs/ShadowStalker.prefab";
        private const float PixelsPerUnit = 224;

        static StalkerSceneSetup()
        {
            if (SessionState.GetBool(PendingKey,false)) EditorApplication.delayCall += ApplyPending;
        }

        public static void ApplyAfterStopping()
        {
            SessionState.SetBool(PendingKey,true);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += ApplyPending;
        }

        private static void ApplyPending()
        {
            if (!SessionState.GetBool(PendingKey,false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += ApplyPending; return; }
            SessionState.SetBool(PendingKey,false);
            Apply();
        }

        [MenuItem("Emergency Shift/Add or update stalker")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Остановите Play перед изменением сцены.");
            SessionState.SetBool(PendingKey,false);
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath)
                throw new InvalidOperationException("Откройте BasementPrototype.");
            Flashlight2D lamp = UnityEngine.Object.FindAnyObjectByType<Flashlight2D>();
            if (lamp == null) throw new InvalidOperationException("В сцене нет фонаря игрока.");
            Directory.CreateDirectory(Art + "/Frames");
            AssetDatabase.Refresh();
            Texture2D walkTexture = ImportTexture(Art + "/WalkSheet.png");
            Texture2D reactionTexture = ImportTexture(Art + "/LightReactionSheet.png");
            if (walkTexture.width != 1536 || walkTexture.height != 1024 || reactionTexture.width != 1254 || reactionTexture.height != 1254)
                throw new InvalidOperationException("Размеры листов изменились; сначала обновите прямоугольники кадров.");

            // Границы найдены по связному силуэту. Равная сетка обрезала правую ногу кадра 5
            // и добавляла оторванный фрагмент в кадр 6. Исходный рисунок сохраняется целиком.
            Rect[] walkRects = {
                new Rect(32,86,353,402),new Rect(417,82,346,412),new Rect(780,82,353,408),new Rect(1165,98,339,396),
                new Rect(33,574,368,383),new Rect(415,574,346,385),new Rect(779,566,352,397),new Rect(1163,560,347,403) };
            Vector2[] walkAnchors = {
                new Vector2(185,198),new Vector2(165,202),new Vector2(174,200),new Vector2(160,186),
                new Vector2(184,184),new Vector2(167,185),new Vector2(175,194),new Vector2(162,199) };
            var walk = new Sprite[8];
            for (int i = 0; i < walk.Length; i++)
                walk[i] = Frame("Walk" + i.ToString("00") + "-corrected", walkTexture, walkRects[i], walkAnchors[i], PixelsPerUnit);
            Rect[] reactionRects = {
                new Rect(20,62,592,575),new Rect(707,142,492,474),new Rect(127,768,398,397),new Rect(784,789,341,319) };
            Vector2[] reactionAnchors = { new Vector2(289,286),new Vector2(226,218),new Vector2(186,171),new Vector2(148,150) };
            var reaction = new Sprite[4];
            for (int i = 0; i < reaction.Length; i++)
                reaction[i] = Frame("Recoil" + i.ToString("00") + "-corrected", reactionTexture, reactionRects[i], reactionAnchors[i], PixelsPerUnit / 0.46f);

            int worldLayer = EnsureObstacleLayer();
            AssignObstacleLayers(worldLayer);
            Material lit = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
            if (lit == null) throw new InvalidOperationException("Не найден материал 2D-света.");
            Sprite disc = CreateEyeDisc();
            ShadowStalker2D actor = UnityEngine.Object.FindAnyObjectByType<ShadowStalker2D>();
            if (actor == null)
            {
                var obj = new GameObject("Stalker — six legs");
                obj.transform.position = new Vector3(-1.6f, -1.5f, 0);
                var rb = obj.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.mass = 2;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var collider = obj.AddComponent<CircleCollider2D>();
                collider.radius = 0.58f;
                collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/EmergencyShift/Materials/NoFriction.physicsMaterial2D");
                actor = obj.AddComponent<ShadowStalker2D>();
                var animator = obj.AddComponent<StalkerSpriteAnimator>();
                Transform visual = new GameObject("Visual", typeof(SortingGroup)).transform;
                visual.SetParent(obj.transform, false);
                visual.GetComponent<SortingGroup>().sortingOrder = 1;
                SpriteRenderer body = Part("Body", visual, walk[0], lit, Color.white, 0, 1);
                SpriteRenderer iris = Part("Eye iris", visual, disc, lit, new Color(0.80f,0.69f,0.72f), 1, 0.108f);
                SpriteRenderer pupil = Part("Eye pupil", visual, disc, lit, new Color(0.025f,0.015f,0.025f), 2, 0.05f);
                animator.Configure(actor, visual, body, iris, pupil, walk, reaction);
                actor.Configure(null, null, 1 << worldLayer);
                PrefabUtility.SaveAsPrefabAssetAndConnect(obj, PrefabPath, InteractionMode.AutomatedAction);
            }
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UpdateFrames(prefabContents,walk,reaction);
                PrefabUtility.SaveAsPrefabAsset(prefabContents,PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefabContents); }
            UpdateFrames(actor.gameObject,walk,reaction);
            // Внешние ссылки принадлежат экземпляру уровня, а не префабу.
            actor.Configure(lamp.transform, lamp, 1 << worldLayer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            EditorUtility.SetDirty(actor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = actor.gameObject;
            Debug.Log("STALKER_READY: eight walking sprites, four recoil sprites, tracked pupil, obstacle layer " + worldLayer);
        }

        private static void UpdateFrames(GameObject root, Sprite[] walk, Sprite[] recoil)
        {
            var actor = root.GetComponent<ShadowStalker2D>();
            var animator = root.GetComponent<StalkerSpriteAnimator>();
            Transform visual = root.transform.Find("Visual");
            var body = visual.Find("Body").GetComponent<SpriteRenderer>();
            animator.Configure(actor,visual,body,visual.Find("Eye iris").GetComponent<SpriteRenderer>(),
                visual.Find("Eye pupil").GetComponent<SpriteRenderer>(),walk,recoil);
            body.sprite = walk[0];
            EditorUtility.SetDirty(animator); EditorUtility.SetDirty(body);
            if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            }
        }

        internal static Texture2D ImportTexture(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Не найден лист " + path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Координаты исходного PNG идут сверху вниз, а Rect/Pivot Unity — снизу вверх.
        internal static Sprite Frame(string name, Texture2D texture, Rect fromTop, Vector2 anchor, float ppu)
        {
            string path = Art + "/Frames/" + name + ".asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            Rect rect = new Rect(fromTop.x, texture.height - fromTop.y - fromTop.height, fromTop.width, fromTop.height);
            Vector2 pivot = new Vector2(anchor.x / fromTop.width, (fromTop.height - anchor.y) / fromTop.height);
            Sprite sprite = Sprite.Create(texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }

        private static SpriteRenderer Part(string name, Transform parent, Sprite sprite, Material material, Color color, int order, float scale)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = Vector3.one * scale;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }

        private static Sprite CreateEyeDisc()
        {
            string path = Art + "/EyeDisc.asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int side = 128;
            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false) { name = "Eye disc", filterMode = FilterMode.Bilinear };
            var pixels = new Color[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float radius = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(side/2f,side/2f));
                    pixels[y*side+x] = new Color(1,1,1,Mathf.Clamp01((side/2f-radius)*2));
                }
            texture.SetPixels(pixels); texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0,0,side,side), new Vector2(0.5f,0.5f), side);
            sprite.name = "Eye disc";
            AssetDatabase.CreateAsset(sprite, path);
            AssetDatabase.AddObjectToAsset(texture, sprite);
            return sprite;
        }

        private static int EnsureObstacleLayer()
        {
            int existing = LayerMask.NameToLayer("LightObstacles");
            if (existing >= 0) return existing;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = settings.FindProperty("layers");
            for (int i = 8; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = "LightObstacles";
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            throw new InvalidOperationException("Нет свободного пользовательского слоя для препятствий.");
        }

        private static void AssignObstacleLayers(int layer)
        {
            foreach (string prefabPath in new[] { "Assets/EmergencyShift/Prefabs/Wall.prefab", "Assets/EmergencyShift/Prefabs/Door.prefab" })
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    foreach (ShadowCaster2D caster in contents.GetComponentsInChildren<ShadowCaster2D>())
                        if (caster.GetComponent<Collider2D>() != null) caster.gameObject.layer = layer;
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            foreach (ShadowCaster2D caster in UnityEngine.Object.FindObjectsByType<ShadowCaster2D>())
                if (caster.GetComponent<Collider2D>() != null)
                {
                    caster.gameObject.layer = layer;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(caster.gameObject);
                }
        }
    }
}
