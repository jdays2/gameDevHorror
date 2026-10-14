using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>One-time authoring tool. Saves real references in the prefab and scene; no runtime builder.</summary>
    public static class StalkerAudioSetup
    {
        private const string ClipFolder = "Assets/EmergencyShift/Audio/Stalker/Membrane04";
        private const string SetPath = "Assets/EmergencyShift/Audio/Stalker/StalkerMembrane.asset";
        private const string PrefabPath = "Assets/EmergencyShift/Prefabs/ShadowStalker.prefab";

        [MenuItem("Emergency Shift/Audio/Apply approved Stalker audio")]
        private static void ApplyMenu() { Debug.Log(Apply()); }

        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play before authoring audio references.");
            var before = new Dictionary<ShadowStalker2D, string>();
            foreach (var actor in UnityEngine.Object.FindObjectsByType<ShadowStalker2D>()) before.Add(actor, EditorJsonUtility.ToJson(actor));
            var set = CreateSet();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                string original = EditorJsonUtility.ToJson(root.GetComponent<ShadowStalker2D>());
                Ensure(root, set);
                if (EditorJsonUtility.ToJson(root.GetComponent<ShadowStalker2D>()) != original)
                    throw new InvalidOperationException("Audio setup changed prefab AI data.");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            int count = 0;
            foreach (var entry in before)
            {
                Ensure(entry.Key.gameObject, set);
                if (EditorJsonUtility.ToJson(entry.Key) != entry.Value)
                    throw new InvalidOperationException("Audio setup changed scene AI data: " + entry.Key.name);
                count++;
            }
            if (UnityEngine.Object.FindObjectsByType<AudioListener>().Length == 0)
            {
                if (Camera.main == null) throw new InvalidOperationException("Main Camera is required for the audio listener.");
                Camera.main.gameObject.AddComponent<AudioListener>();
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            return "Approved membrane set assigned to prefab and " + count + " scene stalkers; AI data preserved.";
        }

        private static StalkerAudioSet CreateSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<StalkerAudioSet>(SetPath);
            if (set != null) return set; // Preserve subsequent Inspector edits.
            set = ScriptableObject.CreateInstance<StalkerAudioSet>();
            AssetDatabase.CreateAsset(set, SetPath);
            var data = new SerializedObject(set);
            string[] fields = { "step", "scrape", "idle", "notice", "windup", "lunge", "bite", "lightFear", "retreatPain", "cower", "hit", "death", "debris" };
            string[] names = { "step", "scrape", "idle", "notice", "windup", "lunge", "bite", "light_fear", "retreat_pain", "cower", "hit", "death", "debris" };
            for (int i = 0; i < fields.Length; i++)
            {
                var paths = new List<string>();
                foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { ClipFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileName(path).StartsWith("stalker_" + names[i] + "_", StringComparison.Ordinal)) paths.Add(path);
                }
                paths.Sort(StringComparer.Ordinal);
                if (paths.Count == 0) throw new InvalidOperationException("Missing clips for " + fields[i]);
                var array = data.FindProperty(fields[i]); array.arraySize = paths.Count;
                for (int j = 0; j < paths.Count; j++) array.GetArrayElementAtIndex(j).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[j]);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); return set;
        }

        private static void Ensure(GameObject root, StalkerAudioSet set)
        {
            var component = root.GetComponent<StalkerAudio2D>();
            if (component == null) component = root.AddComponent<StalkerAudio2D>();
            var audio = Child(root.transform, "Audio");
            var movement = Source(Child(audio, "Movement"), 140);
            var voice = Source(Child(audio, "Voice"), 64);
            var data = new SerializedObject(component);
            AssignIfMissing(data, "sounds", set);
            AssignIfMissing(data, "movementSource", movement);
            AssignIfMissing(data, "voiceSource", voice);
            AssignIfMissing(data, "movementFilter", movement.GetComponent<AudioLowPassFilter>());
            AssignIfMissing(data, "voiceFilter", voice.GetComponent<AudioLowPassFilter>());
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        private static void AssignIfMissing(SerializedObject data, string name, UnityEngine.Object value)
        {
            var field = data.FindProperty(name);
            if (field.objectReferenceValue == null) field.objectReferenceValue = value;
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            return child;
        }

        private static AudioSource Source(Transform child, int priority)
        {
            var source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
                source.volume = 0; source.dopplerLevel = 0; source.priority = priority;
            }
            if (child.GetComponent<AudioLowPassFilter>() == null)
                child.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 22000;
            return source;
        }
    }
}
