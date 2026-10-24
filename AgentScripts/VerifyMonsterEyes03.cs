using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyMonsterEyes03
{
    public static string Read()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Inspect saved eye assets with Play stopped.");
        }

        var prefabs = new List<object>();
        foreach (string species in new[] { "ShadowStalker", "Sleeper" })
        {
            string path = "Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int missingScripts = root.GetComponentsInChildren<Transform>(true)
                    .Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject));
                var animator = new SerializedObject(root.GetComponent<CreatureSpriteAnimator2D>());
                var pupil = (SpriteRenderer)animator.FindProperty("pupil").objectReferenceValue;
                var burst = new SerializedObject(root.GetComponent<GoreBurst2D>());
                var profile = (GoreProfile)burst.FindProperty("profile").objectReferenceValue;
                profile.Validate();
                GoreVariant[] active = Enumerable.Range(0, profile.VariantCount).Select(profile.VariantAt)
                    .Where(value => !value.RestorationOnly).ToArray();
                int weight = active.Sum(value => value.SelectionWeight);
                int eyeWeight = active.Where(value => Enumerable.Range(0, value.PieceCount)
                    .Any(index => value.PieceAt(index).EyeSurface)).Sum(value => value.SelectionWeight);
                int missingReferences = 0;
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(component);
                    SerializedProperty field = serialized.GetIterator();
                    while (field.NextVisible(true))
                    {
                        if (field.propertyType == SerializedPropertyType.ObjectReference &&
                            field.objectReferenceValue == null && field.objectReferenceEntityIdValue != default)
                        {
                            missingReferences++;
                        }
                    }
                }

                prefabs.Add(new
                {
                    path,
                    guid = AssetDatabase.AssetPathToGUID(path),
                    missingScripts,
                    missingReferences,
                    pupilScale = pupil.transform.localScale.x,
                    warningExpansion = animator.FindProperty("pupilWarningExpansion").floatValue,
                    eyeChance = (float)eyeWeight / weight,
                    activeCompositions = active.Length,
                    fragments = burst.FindProperty("fragments").arraySize,
                    settledTint = new[] { profile.SettledTint.r, profile.SettledTint.g, profile.SettledTint.b },
                    eyeMaterial = AssetDatabase.GetAssetPath(profile.EyeSurfaceMaterial)
                });
                if (missingScripts != 0 || missingReferences != 0 || Mathf.Abs(pupil.transform.localScale.x - .1105f) > .00001f ||
                    Mathf.Abs((float)eyeWeight / weight - .35f) > .00001f)
                {
                    throw new InvalidOperationException("Saved eye assets failed verification: " + path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/EmergencyShift/Materials/Characters/LocalizedEyeBurn.shader");
        var messages = ShaderUtil.GetShaderMessages(shader).Select(value => new
        {
            value.message,
            value.line,
            severity = value.severity.ToString()
        }).ToArray();
        if (messages.Any(value => value.severity == "Error"))
        {
            throw new InvalidOperationException("Eye shader has errors.");
        }

        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .Select(value => new { value.path, value.isDirty }).ToArray();
        var result = new
        {
            prefabs,
            shaderMessages = messages,
            openScenes = scenes,
            buildScenes = EditorBuildSettings.scenes.Where(value => value.enabled).Select(value => value.path).ToArray(),
            playModeStartScene = EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : null,
            sceneAuthoringGate = "Not applicable: no saved scene object additions, moves or reparenting. Runtime probes discarded on Stop."
        };
        Directory.CreateDirectory("Docs/Validation/MonsterEyes03");
        File.WriteAllText("Docs/Validation/MonsterEyes03/editor-assets.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return "Docs/Validation/MonsterEyes03/editor-assets.json";
    }
}
