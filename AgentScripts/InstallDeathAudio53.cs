using System;
using EmergencyShift.Features.WorldAudio;
using EmergencyShift.Features.WorldAudio.Config;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

public static class InstallDeathAudio53
{
    private const string ProfilePath = "Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset";
    private const string ContextPath = "Assets/EmergencyShift/Resources/ProjectContext.prefab";

    public static object Install()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires an idle EditMode Editor.");
        }
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/EmergencyShift/Data/Audio/WorldAudio.mixer");
        if (mixer == null || !mixer.GetFloat("MyExposedParam", out _))
        {
            throw new InvalidOperationException("Create the native mixer and expose Master Volume first.");
        }
        var profile = AssetDatabase.LoadAssetAtPath<WorldAudioProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<WorldAudioProfile>();
            using (var data = new SerializedObject(profile))
            {
                data.FindProperty("worldOutput").objectReferenceValue = mixer.FindMatchingGroups("Master")[0];
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            profile.Validate();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        GameObject root = PrefabUtility.LoadPrefabContents(ContextPath);
        try
        {
            var installer = root.GetComponent<WorldAudioInstaller>();
            if (installer == null)
            {
                installer = root.AddComponent<WorldAudioInstaller>();
            }
            using (var data = new SerializedObject(installer))
            {
                data.FindProperty("profile").objectReferenceValue = profile;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            using (var data = new SerializedObject(root.GetComponent<ProjectContext>()))
            {
                SerializedProperty installers = data.FindProperty("_monoInstallers");
                bool found = false;
                for (int index = 0; index < installers.arraySize; index++)
                {
                    found |= installers.GetArrayElementAtIndex(index).objectReferenceValue == installer;
                }
                if (!found)
                {
                    int index = installers.arraySize++;
                    installers.GetArrayElementAtIndex(index).objectReferenceValue = installer;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, ContextPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssetIfDirty(profile);
        return new { profile = ProfilePath, context = ContextPath, mixer = AssetDatabase.GetAssetPath(mixer),
            sceneObjectsAddedOrMoved = false, profile.ToSettings().FadeOutSeconds, profile.ToSettings().RestoreSeconds };
    }
}
