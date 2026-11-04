using System;
using System.Collections.Generic;
using System.IO;
using EmergencyShift.Features.Pickups;
using EmergencyShift.Features.Pickups.Config;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject;

// Explicit one-time migration. Compile the proposed installer first; game lease required.
// Saves only ProjectContext.prefab. Never loads or saves an authored game scene.
public static class Task008PickupComposition
{
    private const string PrefabPath = "Assets/EmergencyShift/Resources/ProjectContext.prefab";
    private const string ProfilePath = "Assets/EmergencyShift/Resources/PickupGlintProfile.asset";

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            throw new InvalidOperationException("Requires idle stopped Editor.");
        }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            throw new InvalidOperationException("Close the existing Prefab Stage without losing its edits first.");
        }
        string output = Path.GetFullPath("Docs/Validation/CodeReadabilityTask008/pickup-composition.install.json");
        if (File.Exists(output))
        {
            throw new IOException("Preserve existing installation evidence.");
        }
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var profile = AssetDatabase.LoadAssetAtPath<PickupGlintProfile>(ProfilePath);
        if (asset == null || profile == null || EditorUtility.IsDirty(profile))
        {
            throw new InvalidOperationException("Requires the existing clean prefab/profile assets.");
        }
        foreach (UnityEngine.Object item in AssetDatabase.LoadAllAssetsAtPath(PrefabPath))
        {
            if (EditorUtility.IsDirty(item))
            {
                throw new InvalidOperationException("ProjectContext has pre-existing unsaved asset edits.");
            }
        }
        profile.Validate();
        string profileBefore = EditorJsonUtility.ToJson(profile);
        string guidBefore = AssetDatabase.AssetPathToGUID(PrefabPath);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int oldInstallerCount;
        var originalComponents = new Dictionary<Component, string>();
        try
        {
            var context = root.GetComponent<ProjectContext>();
            if (context == null || root.GetComponentsInChildren<PickupGlintInstaller>(true).Length != 0)
            {
                throw new InvalidOperationException("Expected an existing ProjectContext without a glint installer.");
            }
            var serialized = new SerializedObject(context);
            var installers = serialized.FindProperty("_monoInstallers");
            if (installers == null || !installers.isArray)
            {
                throw new InvalidOperationException("ProjectContext installer list is missing.");
            }
            oldInstallerCount = installers.arraySize;
            var oldInstallers = new List<UnityEngine.Object>();
            for (int i = 0; i < oldInstallerCount; i++)
            {
                var value = installers.GetArrayElementAtIndex(i).objectReferenceValue;
                if (value == null)
                {
                    throw new InvalidOperationException("Existing installer reference is missing.");
                }
                oldInstallers.Add(value);
            }
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    throw new InvalidOperationException("Existing prefab has a missing script.");
                }
                if (component != context)
                {
                    originalComponents.Add(component, EditorJsonUtility.ToJson(component));
                }
            }
            var installer = root.AddComponent<PickupGlintInstaller>();
            var installerData = new SerializedObject(installer);
            installerData.FindProperty("profile").objectReferenceValue = profile;
            installerData.ApplyModifiedPropertiesWithoutUndo();
            installers.arraySize = oldInstallerCount + 1;
            installers.GetArrayElementAtIndex(oldInstallerCount).objectReferenceValue = installer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < oldInstallerCount; i++)
            {
                if (installers.GetArrayElementAtIndex(i).objectReferenceValue != oldInstallers[i])
                {
                    throw new InvalidOperationException("An existing installer changed.");
                }
            }
            foreach (var original in originalComponents)
            {
                if (EditorJsonUtility.ToJson(original.Key) != original.Value)
                {
                    throw new InvalidOperationException("An existing component changed: " + original.Key.GetType().Name);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved)
            {
                throw new IOException("ProjectContext prefab save failed.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        var savedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var savedInstallers = savedAsset.GetComponents<PickupGlintInstaller>();
        var savedContext = new SerializedObject(savedAsset.GetComponent<ProjectContext>());
        var savedList = savedContext.FindProperty("_monoInstallers");
        if (savedInstallers.Length != 1 || savedList.arraySize != oldInstallerCount + 1 ||
            savedList.GetArrayElementAtIndex(oldInstallerCount).objectReferenceValue != savedInstallers[0] ||
            new SerializedObject(savedInstallers[0]).FindProperty("profile").objectReferenceValue != profile ||
            EditorJsonUtility.ToJson(profile) != profileBefore ||
            AssetDatabase.AssetPathToGUID(PrefabPath) != guidBefore)
        {
            throw new InvalidOperationException("Saved composition validation failed.");
        }
        var report = new JObject
        {
            ["installed"] = true,
            ["prefab"] = PrefabPath,
            ["prefabGuid"] = guidBefore,
            ["profile"] = ProfilePath,
            ["profileGuid"] = AssetDatabase.AssetPathToGUID(ProfilePath),
            ["previousInstallers"] = oldInstallerCount,
            ["currentInstallers"] = savedList.arraySize,
            ["originalComponentsUnchanged"] = originalComponents.Count,
            ["sceneSaves"] = 0
        };
        File.WriteAllText(output, report.ToString());
        return report;
    }
}
