using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Authors saved channels and data without rebuilding the level or changing loot/AI settings.</summary>
    public static class PlayerAudioSetup
    {
        private const string Folder = "Assets/EmergencyShift/Audio/PlayerPrototype";
        private const string SetPath = Folder + "/PlayerPrototypeAudio.asset";

        [MenuItem("Emergency Shift/Audio/Configure pickup and candidate weapon audio")]
        private static void ApplyMenu() { Debug.Log(Apply()); }

        public static string Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play before saving audio references.");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
            if (player == null) throw new InvalidOperationException("PlayerInteraction is required.");
            var set = CreateSet();
            var component = player.GetComponent<PlayerAudio2D>();
            if (component == null) component = player.gameObject.AddComponent<PlayerAudio2D>();
            var root = Child(player.transform, "Audio");
            var data = new SerializedObject(component);
            AssignIfMissing(data, "sounds", set);
            AssignIfMissing(data, "pickupSource", Source(Child(root, "Pickup"), 80));
            AssignIfMissing(data, "shotSource", Source(Child(root, "Shot"), 48));
            var impacts = data.FindProperty("impactSources");
            if (impacts.arraySize == 0)
            {
                impacts.arraySize = 4;
                for (int i = 0; i < 4; i++)
                    impacts.GetArrayElementAtIndex(i).objectReferenceValue = Source(Child(root, "Impact " + (i + 1)), 100);
            }
            // Deliberately preserve Inspector choices, including the default-disabled combat audition.
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            return "Saved pickup channel, shot channel and four impact channels. Combat candidates remain opt-in.";
        }

        private static PlayerAudioSet CreateSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayerAudioSet>(SetPath);
            if (set != null) return set;
            set = ScriptableObject.CreateInstance<PlayerAudioSet>();
            AssetDatabase.CreateAsset(set, SetPath);
            var data = new SerializedObject(set);
            string[] fields = { "ammoPickup", "weaponPickup", "upgradePickup", "dryShots", "roomShots", "concreteImpact", "woodImpact", "fleshImpact" };
            string[] prefixes = { "pickup_ammo_", "pickup_weapon_", "pickup_upgrade_", "shot_dry_", "shot_room_", "impact_concrete_", "impact_wood_", "stalker_hit_" };
            for (int i = 0; i < fields.Length; i++)
            {
                string folder = i == 7 ? "Assets/EmergencyShift/Audio/Stalker/Membrane04" : Folder;
                var paths = new List<string>();
                foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileName(path).StartsWith(prefixes[i], StringComparison.Ordinal)) paths.Add(path);
                }
                paths.Sort(StringComparer.Ordinal);
                if (paths.Count == 0) throw new InvalidOperationException("Missing clips: " + fields[i]);
                var array = data.FindProperty(fields[i]); array.arraySize = paths.Count;
                for (int j = 0; j < paths.Count; j++) array.GetArrayElementAtIndex(j).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[j]);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return set;
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
            return source;
        }
    }
}
