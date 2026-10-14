using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Сохраняет неподвижные тестовые останки в сцене; в Play ничего не создаёт.</summary>
    public static class StalkerCorpseSetup
    {
        private const string RootName = "Test corpses — Stalkers";
        private const string Art = "Assets/EmergencyShift/Art/Stalker/Frames/";
        private const string CorpsePath = "Assets/EmergencyShift/Prefabs/StalkerRemainsTest.prefab";

        [MenuItem("Emergency Shift/Preview/Add four test corpses")]
        public static void Add()
        {
            RequireScene();
            // Повторный вызов не дублирует группу и не сбрасывает её ручную расстановку.
            if (GameObject.Find(RootName) != null) return;
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
            var corpse = AssetDatabase.LoadAssetAtPath<GameObject>(CorpsePath);
            if (corpse == null)
            {
                var template = new GameObject("Stalker remains — test");
                var visual = new GameObject("Remains", typeof(SortingGroup));
                visual.transform.SetParent(template.transform, false);
                visual.GetComponent<SortingGroup>().sortingOrder = 1;
                Part("Body", visual.transform, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Death03.asset"), material);
                for (int i = 0; i < 8; i++)
                    Part("Fragment " + i, visual.transform, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Gib" + (i % 6).ToString("00") + ".asset"), material);
                RemainsVisualStyle.Apply(visual.transform);
                var poolAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/BloodPool.prefab");
                var pool = (GameObject)PrefabUtility.InstantiatePrefab(poolAsset);
                pool.transform.SetParent(template.transform, false);
                // При равном sortingOrder отделяем кровь от пола так же, как живой GoreBurst.
                pool.transform.localPosition = new Vector3(0, 0, -0.02f);
                corpse = PrefabUtility.SaveAsPrefabAsset(template, CorpsePath);
                UnityEngine.Object.DestroyImmediate(template);
            }
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add test Stalker corpses");
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Add test corpse group");
            Vector2[] positions = { new Vector2(-0.8f, 1.15f), new Vector2(1.35f, 0.75f),
                new Vector2(-1.5f, -1.2f), new Vector2(0.9f, -1.5f) };
            Physics2D.SyncTransforms();
            for (int i = 0; i < positions.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(corpse);
                Undo.RegisterCreatedObjectUndo(instance, "Add test corpse");
                instance.name = "Dead Stalker " + (i + 1);
                instance.transform.SetParent(root.transform, false);
                instance.transform.position = positions[i];
                var random = new System.Random(4100 + i * 73);
                Transform visual = instance.transform.Find("Remains");
                Transform body = visual.Find("Body");
                body.localRotation = Quaternion.Euler(0, 0, (float)random.NextDouble() * 360);
                body.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, (float)random.NextDouble());
                for (int j = 0; j < 8; j++)
                {
                    Transform piece = visual.Find("Fragment " + j);
                    float angle = (j + (float)random.NextDouble()) * Mathf.PI * 2 / 8;
                    float radius = Mathf.Lerp(0.55f, 1.3f, (float)random.NextDouble());
                    piece.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                    piece.localRotation = Quaternion.Euler(0, 0, (float)random.NextDouble() * 360);
                    piece.localScale = Vector3.one * Mathf.Lerp(0.65f, 0.95f, (float)random.NextDouble());
                }
                instance.GetComponentInChildren<BloodPool2D>().Initialize(1.05f + i * 0.1f, 4100 + i * 73, 1 << 8);
                foreach (Transform child in instance.GetComponentsInChildren<Transform>())
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child);
                foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>())
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            Undo.CollapseUndoOperations(undoGroup);
            Save();
            Selection.activeGameObject = root;
            Debug.Log("STALKER_TEST_CORPSES_READY: four static corpses, four pools, 32 fragments");
        }

        private static void Part(string name, Transform parent, Sprite sprite, Material material)
        {
            if (sprite == null) throw new InvalidOperationException("Missing corpse sprite: " + name);
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sharedMaterial = material;
        }

        [MenuItem("Emergency Shift/Preview/Remove test corpses")]
        public static void Remove()
        {
            RequireScene();
            var root = GameObject.Find(RootName);
            if (root == null) return;
            Undo.DestroyObjectImmediate(root);
            Save();
        }

        private static void RequireScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Остановите Play.");
            if (SceneManager.GetActiveScene().path != PrototypeSceneBuilder.ScenePath)
                throw new InvalidOperationException("Откройте BasementPrototype.");
        }
        private static void Save()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
    }
}
