using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Создаёт плейсхолдер-префаб и подключает игрока/HUD, без расстановки лута в сцене.</summary>
    public static class FlashlightUpgradeSetup
    {
        public const string PrefabPath = "Assets/EmergencyShift/Prefabs/FlashlightRangeUpgrade.prefab";

        [MenuItem("Emergency Shift/Preview/Add two flashlight upgrades")]
        public static void AddTwoRepeatableUpgrades()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Остановите Play.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath) throw new InvalidOperationException("Откройте BasementPrototype.");
            Apply();
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var settings = new SerializedObject(contents.GetComponent<FlashlightUpgradePickup>());
                // Обновляем первоначальный плейсхолдер, сохраняя отличающиеся ручные бонусы.
                if (Mathf.Approximately(settings.FindProperty("rangeBonus").floatValue, 0.8f)) settings.FindProperty("rangeBonus").floatValue = 0.6f;
                if (Mathf.Approximately(settings.FindProperty("angleBonus").floatValue, 0)) settings.FindProperty("angleBonus").floatValue = 6f;
                if (settings.FindProperty("displayName").stringValue == "Фокусирующая линза") settings.FindProperty("displayName").stringValue = "Модуль фонаря";
                settings.FindProperty("repeatable").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            // Уже расставленный предмет не перемещаем; ручные overrides сохраняются.
            foreach (FlashlightUpgradePickup item in UnityEngine.Object.FindObjectsByType<FlashlightUpgradePickup>(FindObjectsInactive.Include))
            {
                var settings = new SerializedObject(item);
                settings.FindProperty("repeatable").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(item)) PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                EditorUtility.SetDirty(item);
            }
            const string groupName = "Flashlight upgrades — test path";
            GameObject group = GameObject.Find(groupName) ?? new GameObject(groupName);
            AddPickup(group.transform, "Flashlight upgrade — 2", new Vector3(0, -0.7f, -0.045f));
            AddPickup(group.transform, "Flashlight upgrade — 3", new Vector3(12, 1f, -0.045f));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        private static void AddPickup(Transform parent, string name, Vector3 position)
        {
            if (parent.Find(name) != null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.transform.SetParent(parent, true); obj.name = name; obj.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        }

        [MenuItem("Emergency Shift/Prepare flashlight upgrade")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Остановите Play.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath) throw new InvalidOperationException("Откройте BasementPrototype.");
            EnsurePickupPrefab();
            const string playerPath = "Assets/EmergencyShift/Prefabs/Player.prefab";
            GameObject playerPrefab = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                EnsurePlayer(playerPrefab);
                PrefabUtility.SaveAsPrefabAsset(playerPrefab, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(playerPrefab); }
            foreach (PlayerInteraction player in UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsInactive.Include))
                EnsurePlayer(player.gameObject);
            foreach (PrototypeHud hud in UnityEngine.Object.FindObjectsByType<PrototypeHud>(FindObjectsInactive.Include))
                EnsureNotification(hud);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("FLASHLIGHT_UPGRADE_READY: drag FlashlightRangeUpgrade.prefab into the Scene; illuminate, approach, E.");
        }

        private static void EnsurePlayer(GameObject player)
        {
            if (player.GetComponent<FlashlightUpgrades>() == null) player.AddComponent<FlashlightUpgrades>();
            // Оптику/позиции и пользовательские поля остальных компонентов не меняем.
        }

        private static void EnsurePickupPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Shapes/Rectangle.asset");
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
            if (sprite == null || material == null) throw new InvalidOperationException("Отсутствуют базовые форма/материал.");
            var root = new GameObject("Flashlight range upgrade", typeof(CircleCollider2D), typeof(FlashlightUpgradePickup));
            try
            {
                root.transform.position = new Vector3(0, 0, -0.045f);
                var trigger = root.GetComponent<CircleCollider2D>(); trigger.radius = 0.2f; trigger.isTrigger = true;
                AddVisual(root.transform, "Module shell", sprite, material, 0.32f, new Color(0.75f, 0.81f, 0.80f), 0);
                AddVisual(root.transform, "Lens", sprite, material, 0.16f, new Color(0.40f, 0.74f, 0.82f), -0.001f);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void AddVisual(Transform parent, string name, Sprite sprite, Material material, float size, Color color, float z)
        {
            var part = new GameObject(name, typeof(SpriteRenderer)); part.transform.SetParent(parent, false);
            part.transform.localPosition = new Vector3(0, 0, z);
            part.transform.localRotation = Quaternion.Euler(0, 0, 45);
            part.transform.localScale = Vector3.one * size;
            var renderer = part.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = material; renderer.color = color; renderer.sortingOrder = 0;
        }

        private static void EnsureNotification(PrototypeHud hud)
        {
            Transform existing = hud.transform.Find("Pickup notification");
            Text label;
            if (existing != null) label = existing.GetComponent<Text>();
            else
            {
                var obj = new GameObject("Pickup notification", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                obj.transform.SetParent(hud.transform, false); label = obj.GetComponent<Text>();
                RectTransform rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = new Vector2(0, 138);
                rect.sizeDelta = new Vector2(760, 42);
                Text source = hud.transform.Find("Door prompt").GetComponent<Text>();
                label.font = source.font; label.fontSize = 18; label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(0.80f, 0.92f, 0.92f); label.raycastTarget = false;
                label.text = ""; obj.SetActive(false);
            }
            if (label == null) throw new InvalidOperationException("Pickup notification должен содержать Text.");
            hud.ConfigurePickupNotification(label); EditorUtility.SetDirty(hud);
            if (PrefabUtility.IsPartOfPrefabInstance(hud)) PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        }
    }
}
