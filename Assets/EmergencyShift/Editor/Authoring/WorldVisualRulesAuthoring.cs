using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.Doors.Config;
using EmergencyShift.Features.LampPower.Config;
using UnityEditor;

namespace EmergencyShift.Editor
{
    /// <summary>Editor composition of existing shared profiles; no scene regeneration or creation.</summary>
    public static class WorldVisualRulesAuthoring
    {
        public const string DoorPath = "Assets/EmergencyShift/Data/World/DoorRules.asset";
        public const string FlickerPath = "Assets/EmergencyShift/Data/World/FixtureFlicker.asset";

        public static void ConfigureInstaller(WorldInstaller installer)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || installer == null)
            {
                throw new InvalidOperationException("World profile authoring requires an installer and stable Edit mode.");
            }

            var doors = AssetDatabase.LoadAssetAtPath<DoorRulesProfile>(DoorPath);
            var flicker = AssetDatabase.LoadAssetAtPath<LampFlickerProfile>(FlickerPath);
            if (doors == null || flicker == null)
            {
                throw new InvalidOperationException("Import shared door/flicker assets before world authoring.");
            }

            doors.ToSettings();
            flicker.ToSettings();
            var data = new SerializedObject(installer);
            data.FindProperty("doorRules").objectReferenceValue = doors;
            data.FindProperty("fixtureFlicker").objectReferenceValue = flicker;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
