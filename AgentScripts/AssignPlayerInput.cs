using System;
using System.Collections.Generic;
using EmergencyShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class AssignPlayerInput
{
    private const string ControlsPath = "Assets/EmergencyShift/Settings/EmergencyShiftControls.inputactions";
    private const string RigPath = "Assets/EmergencyShift/Prefabs/Levels/LevelRig.prefab";
    private const string BasementPath = "Assets/EmergencyShift/Scenes/BasementPrototype.unity";

    public static object Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Input authoring requires stopped, compiled Edit mode.");
        }

        Scene loadedBasement = SceneManager.GetSceneByPath(BasementPath);
        if (loadedBasement.IsValid() && loadedBasement.isLoaded)
        {
            throw new InvalidOperationException("Close Basement before this disk authoring command; its live state was left intact.");
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BasementPath) == null)
        {
            throw new InvalidOperationException("The target Basement scene must exist before changing its rig.");
        }

        InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
        if (controls == null)
        {
            throw new InvalidOperationException("The authored Input Actions asset must be imported first.");
        }

        var changes = new List<string>();
        GameObject prefab = PrefabUtility.LoadPrefabContents(RigPath);
        try
        {
            LevelContext2D context = prefab.GetComponentInChildren<LevelContext2D>(true);
            Assign(context, controls);
            PrefabUtility.SaveAsPrefabAsset(prefab, RigPath);
            changes.Add(RigPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene basement = SceneManager.GetSceneByPath(BasementPath);
        bool opened = !basement.IsValid() || !basement.isLoaded;
        if (!opened && basement.isDirty)
        {
            throw new InvalidOperationException("Basement has unsaved edits; its disk state was not overwritten.");
        }

        if (opened)
        {
            basement = EditorSceneManager.OpenScene(BasementPath, OpenSceneMode.Additive);
        }

        try
        {
            foreach (GameObject root in basement.GetRootGameObjects())
            {
                foreach (LevelContext2D context in root.GetComponentsInChildren<LevelContext2D>(true))
                {
                    Assign(context, controls);
                }
            }

            EditorSceneManager.MarkSceneDirty(basement);
            if (!EditorSceneManager.SaveScene(basement))
            {
                throw new InvalidOperationException("Could not save the targeted Basement input binding.");
            }

            changes.Add(BasementPath);
        }
        finally
        {
            if (opened)
            {
                EditorSceneManager.CloseScene(basement, true);
            }

            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
        }

        return new { controls = ControlsPath, changed = changes, activeSceneSaved = false };
    }

    private static void Assign(LevelContext2D context, InputActionAsset controls)
    {
        if (context == null)
        {
            throw new InvalidOperationException("Missing authored level context.");
        }

        var serialized = new SerializedObject(context);
        SerializedProperty property = serialized.FindProperty("controls");
        if (property == null)
        {
            throw new InvalidOperationException("The updated level context must compile before assignment.");
        }

        property.objectReferenceValue = controls;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
