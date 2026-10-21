using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.AudioMood.Config;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Editor
{
    public static class ThreatSignalAuthoring
    {
        public const string ProfilePath = "Assets/EmergencyShift/Data/Audio/ThreatSignals.asset";
        public const string ContextPath = "Assets/EmergencyShift/Resources/ProjectContext.prefab";

        [MenuItem("Emergency Shift/Architecture/Assign threat signal profile")]
        public static void Configure()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play before assigning world config.");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(ContextPath);
            try
            {
                WorldInstaller installer = root.GetComponent<WorldInstaller>();
                if (installer == null)
                {
                    throw new InvalidOperationException("Existing ProjectContext has no WorldInstaller.");
                }

                ConfigureInstaller(installer);
                PrefabUtility.SaveAsPrefabAsset(root, ContextPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void ConfigureInstaller(WorldInstaller installer)
        {
            if (Application.isPlaying || installer == null)
            {
                throw new InvalidOperationException("An Editor world installer is required.");
            }

            AudioThreatSignalProfile profile = AssetDatabase.LoadAssetAtPath<AudioThreatSignalProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AudioThreatSignalProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.ToSettings();
            var serialized = new SerializedObject(installer);
            serialized.FindProperty("threatSignals").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
