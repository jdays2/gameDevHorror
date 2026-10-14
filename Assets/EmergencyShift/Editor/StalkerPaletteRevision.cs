using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Заменяет данные Sprite без смены GUID и применяет палитру/стиль останков.</summary>
    public static class StalkerPaletteRevision
    {
        [Serializable] private sealed class Frame
        { public string name; public float x, y, width, height, anchorX, anchorY, ppu; }
        [Serializable] private sealed class Layout
        { public Frame[] walk, recoil, attack, death, gibs; }
        private const string Art = "Assets/EmergencyShift/Art/Stalker";
        private const string Palette = Art + "/PaleFlesh";

        [MenuItem("Emergency Shift/Apply pale flesh palette")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Остановите Play.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath) throw new InvalidOperationException("Откройте BasementPrototype.");
            var layout = JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(Palette + "/PaletteFrameLayout.json").text);
            Replace(layout.walk, StalkerSceneSetup.ImportTexture(Palette + "/walk-sheet.png"));
            Replace(layout.recoil, StalkerSceneSetup.ImportTexture(Palette + "/recoil-sheet.png"));
            Replace(layout.attack, StalkerSceneSetup.ImportTexture(Palette + "/attack-sheet.png"));
            Texture2D death = StalkerSceneSetup.ImportTexture(Palette + "/death-sheet.png");
            Replace(layout.death, death); Replace(layout.gibs, death);
            UpdatePrefab("Assets/EmergencyShift/Prefabs/ShadowStalker.prefab", false);
            UpdatePrefab("Assets/EmergencyShift/Prefabs/StalkerRemainsTest.prefab", true);
            foreach (ShadowStalker2D actor in UnityEngine.Object.FindObjectsByType<ShadowStalker2D>(FindObjectsInactive.Include))
                LiveVisual(actor.gameObject);
            GameObject testRoot = GameObject.Find("Test corpses — Stalkers");
            if (testRoot != null)
                foreach (Transform corpse in testRoot.transform)
                {
                    Transform visual = corpse.Find("Remains");
                    if (visual != null) { RemainsVisualStyle.Apply(visual); Record(visual); }
                }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("STALKER_PALETTE_READY: 28 Sprite assets, pale flesh, dark remains, live sorting above dead");
        }

        private static void Replace(Frame[] entries, Texture2D texture)
        {
            foreach (Frame f in entries)
            {
                string path = Art + "/Frames/" + f.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (existing == null) throw new InvalidOperationException("Нет исходного Sprite: " + path);
                var rect = new Rect(f.x, texture.height - f.y - f.height, f.width, f.height);
                var pivot = new Vector2(f.anchorX / f.width, (f.height - f.anchorY) / f.height);
                Sprite replacement = Sprite.Create(texture, rect, pivot, f.ppu, 0, SpriteMeshType.FullRect, Vector4.zero, false);
                replacement.name = existing.name;
                // GUID и fileID ассета остаются прежними: ссылки анимаций/кусков/трупов сохраняются.
                EditorUtility.CopySerialized(replacement, existing);
                EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(replacement);
            }
        }

        private static void UpdatePrefab(string path, bool corpse)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (corpse) RemainsVisualStyle.Apply(root.transform.Find("Remains"));
                else LiveVisual(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void LiveVisual(GameObject root)
        {
            Transform visual = root.transform.Find("Visual");
            visual.GetComponent<SortingGroup>().sortingOrder = RemainsVisualStyle.LiveOrder;
            visual.Find("Eye iris").GetComponent<SpriteRenderer>().color = new Color(0.84f, 0.75f, 0.79f);
            Record(visual);
        }
        private static void Record(Transform root)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(root)) return;
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            var group = root.GetComponent<SortingGroup>();
            if (group != null) PrefabUtility.RecordPrefabInstancePropertyModifications(group);
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>())
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }
}
