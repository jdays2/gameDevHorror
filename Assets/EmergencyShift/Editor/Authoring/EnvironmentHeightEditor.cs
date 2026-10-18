using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift.Editor
{
    /// <summary>Bakes height classes into standard URP settings; no per-frame scene scan or runtime reflection.</summary>
    public static class EnvironmentHeightAuthoring
    {
        public static void Apply(EnvironmentHeight2D height)
        {
            string layer = EnvironmentShadowLayers.Surface(height.heightClass);
            if (SortingLayer.NameToID(layer) == 0)
            {
                throw new InvalidOperationException("Missing environment shadow sorting layers. See ENVIRONMENT_SHADOWS.md.");
            }

            foreach (var renderer in height.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // Nested height metadata owns its own surfaces, e.g. contents above a worktop.
                if (renderer.GetComponentInParent<EnvironmentHeight2D>() == height)
                {
                    renderer.sortingLayerName = layer;
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }

            foreach (var group in height.GetComponentsInChildren<UnityEngine.Rendering.SortingGroup>(true))
            {
                if (group.GetComponentInParent<EnvironmentHeight2D>() == height)
                {
                    group.sortingLayerName = layer;
                    EditorUtility.SetDirty(group);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(group);
                }
            }

            var targets = new List<int> { SortingLayer.NameToID(EnvironmentShadowLayers.Floor), 0 };
            foreach (EnvironmentHeightClass receiver in Enum.GetValues(typeof(EnvironmentHeightClass)))
            {
                if (EnvironmentShadowLayers.CanCastOnto(height.heightClass, receiver))
                {
                    targets.Add(SortingLayer.NameToID(EnvironmentShadowLayers.Surface(receiver)));
                }
            }

            foreach (var caster in height.GetComponentsInChildren<ShadowCaster2D>(true))
            {
                if (caster.GetComponentInParent<EnvironmentHeight2D>() != height)
                {
                    continue;
                }

                // Unity 6000.6 exposes Light2D targets publicly, but caster targets only through serialization.
                var serialized = new SerializedObject(caster);
                var layers = serialized.FindProperty("m_ApplyToSortingLayers");
                if (layers == null)
                {
                    throw new InvalidOperationException("URP ShadowCaster2D serialization changed.");
                }

                layers.arraySize = targets.Count;
                for (int i = 0; i < targets.Count; i++)
                {
                    layers.GetArrayElementAtIndex(i).intValue = targets[i];
                }

                serialized.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(caster);
            }
        }
    }

    [CustomEditor(typeof(EnvironmentHeight2D)), CanEditMultipleObjects]
    public sealed class EnvironmentHeightEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();
            bool apply = GUILayout.Button("Apply height to surfaces and shadows");
            if (changed || apply)
            {
                foreach (EnvironmentHeight2D height in targets)
                {
                    Undo.RecordObjects(height.GetComponentsInChildren<Component>(true), "Set environment shadow height");
                    EnvironmentHeightAuthoring.Apply(height);
                }
            }

            EditorGUILayout.HelpBox("Baked URP height classes. Low shadows cannot cover taller surfaces. Lights must target ES Floor, ES Low, ES Furniture, ES Tall and Default. Default keeps actors and pickups above the environment.", MessageType.Info);
        }
    }
}
