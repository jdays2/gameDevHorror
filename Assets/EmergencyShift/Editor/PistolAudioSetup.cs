using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Explicit migration to the selected #3/#6 recordings. Does not rebuild geometry or change AI.</summary>
    public static class PistolAudioSetup
    {
        private const string Folder = "Assets/EmergencyShift/Audio/PistolIndoor";
        private const string SetPath = Folder + "/PistolIndoorAudio.asset";
        private const string CasingPath = "Assets/EmergencyShift/Prefabs/PistolCasing.prefab";
        [MenuItem("Emergency Shift/Audio/Apply selected indoor pistol and concrete casings")]
        private static void ApplyMenu() { Debug.Log(Apply()); }
        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play before saving audio references.");
            PlayerAudioSetup.Apply();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerAudio2D>();
            var data = new SerializedObject(player);
            var set = AssetDatabase.LoadAssetAtPath<PlayerAudioSet>(SetPath);
            if (set == null)
            {
                set = UnityEngine.Object.Instantiate((PlayerAudioSet)data.FindProperty("sounds").objectReferenceValue);
                set.name = "PistolIndoorAudio"; AssetDatabase.CreateAsset(set, SetPath);
                var content = new SerializedObject(set);
                Clips(content, "dryShots", "pistol_indoor_"); Clips(content, "roomShots", "pistol_indoor_");
                Clips(content, "concreteCasings", "casing_concrete_");
                content.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            }
            bool newlySelected = data.FindProperty("sounds").objectReferenceValue != set;
            data.FindProperty("sounds").objectReferenceValue = set;
            if (newlySelected)
            {
                data.FindProperty("combatAudioEnabled").boolValue = true;
                data.FindProperty("impactAudioEnabled").boolValue = false; // Unapproved generic impact timbres stay off.
                data.FindProperty("indoorShot").boolValue = true;
                data.FindProperty("shotVolume").floatValue = .80f;
                data.FindProperty("casingVolume").floatValue = .30f;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var ejector = player.GetComponent<PlayerCasingEjector2D>();
            if (ejector == null) ejector = player.gameObject.AddComponent<PlayerCasingEjector2D>();
            var serialized = new SerializedObject(ejector);
            if (serialized.FindProperty("casingPrefab").objectReferenceValue == null)
                serialized.FindProperty("casingPrefab").objectReferenceValue = CasingPrefab();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(player); EditorUtility.SetDirty(ejector);
            if (PrefabUtility.IsPartOfPrefabInstance(player)) PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            if (PrefabUtility.IsPartOfPrefabInstance(ejector)) PrefabUtility.RecordPrefabInstancePropertyModifications(ejector);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            return "Selected indoor pistol enabled, six shot edits/six concrete drops, bounded casing prefab and channels saved.";
        }
        private static void Clips(SerializedObject data, string field, string prefix)
        {
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            { string path = AssetDatabase.GUIDToAssetPath(guid); if (System.IO.Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal)) paths.Add(path); }
            paths.Sort(StringComparer.Ordinal);
            if (paths.Count != 6) throw new InvalidOperationException("Expected six clips: " + prefix);
            var array = data.FindProperty(field); array.arraySize = paths.Count;
            for (int i = 0; i < paths.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
        }
        private static ShellCasing2D CasingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ShellCasing2D>(CasingPath);
            if (prefab != null) return prefab;
            var root = new GameObject("Pistol casing");
            try
            {
                var component = root.AddComponent<ShellCasing2D>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Shapes/Rectangle.asset");
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
                var body = new GameObject("Brass").AddComponent<SpriteRenderer>(); body.transform.SetParent(root.transform, false);
                body.sprite = sprite; body.sharedMaterial = material; body.sortingOrder = 0;
                var shadow = new GameObject("Contact shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(root.transform, false);
                shadow.transform.localPosition = new Vector3(.012f, -.012f, .001f);
                shadow.sprite = sprite; shadow.sharedMaterial = material; shadow.sortingOrder = 0;
                var data = new SerializedObject(component); data.FindProperty("body").objectReferenceValue = body;
                data.FindProperty("contact").objectReferenceValue = shadow; data.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, CasingPath).GetComponent<ShellCasing2D>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
