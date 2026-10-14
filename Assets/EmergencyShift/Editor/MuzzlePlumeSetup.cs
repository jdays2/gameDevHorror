using System;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Editor
{
    /// <summary>Updates only the saved shot prefab. Scene layout, profiles and combat tuning are retained.</summary>
    public static class MuzzlePlumeSetup
    {
        [MenuItem("Emergency Shift/Effects/Apply broad muzzle plume")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var root = PrefabUtility.LoadPrefabContents(WeaponPrototypeSetup.EffectPath);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, WeaponPrototypeSetup.EffectPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            for (int i = 0; i < 3; i++)
            {
                var profile = AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(WeaponPrototypeSetup.ProfilePath(i));
                if (profile != null) EditorUtility.SetDirty(profile); // Persist added fields, retain existing tuning.
            }
            AssetDatabase.SaveAssets();
        }

        public static void Configure(GameObject root)
        {
            const string materialPath = "Assets/EmergencyShift/Materials/MuzzlePlume.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("EmergencyShift/Muzzle Plume");
                if (shader == null) throw new InvalidOperationException("Import MuzzlePlume.shader first.");
                material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath);
            }
            var child = root.transform.Find("Fire plume");
            var renderer = child != null ? child.GetComponent<SpriteRenderer>() : new GameObject("Fire plume", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(root.transform, false);
            renderer.transform.localPosition = new Vector3(0, .328f, -.003f);
            renderer.transform.localScale = new Vector3(.95f, .78f, 1);
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Shapes/Rectangle.asset");
            renderer.sharedMaterial = material; renderer.sortingOrder = 2;
            root.GetComponent<ShotEffect2D>().ConfigurePlume(renderer);
            // Keep old named layers as impact/fallback data; muzzle rendering no longer stacks them.
            foreach (var part in root.GetComponentsInChildren<SpriteRenderer>()) part.enabled = part == renderer;
        }
    }
}
