using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.PlayerRules.Config;
using UnityEditor;

namespace EmergencyShift.Editor
{
    /// <summary>Assigns the existing shared player profile. Never creates or resets scene content.</summary>
    public static class PlayerRulesAuthoring
    {
        public const string ProfilePath = "Assets/EmergencyShift/Data/Player/PlayerRules.asset";

        public static void ConfigureInstaller(WorldInstaller installer)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                throw new InvalidOperationException("Player rules authoring requires stable Edit mode.");
            }

            if (installer == null)
            {
                throw new ArgumentNullException(nameof(installer));
            }

            var profile = AssetDatabase.LoadAssetAtPath<PlayerRulesProfile>(ProfilePath);
            if (profile == null)
            {
                throw new InvalidOperationException("Import the shared player rules profile before world authoring.");
            }

            profile.Validate();
            var data = new SerializedObject(installer);
            data.FindProperty("playerRules").objectReferenceValue = profile;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
