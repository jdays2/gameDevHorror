using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>Restores only the three pre-existing light override lists touched by Unity's save.</summary>
public static class RestoreDeathScreenUnrelatedOverrides
{
    public static object Run()
    {
        if (Application.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("One clean stopped scene is required.");
        }
        Scene scene = EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/MechanicsSandbox.unity", OpenSceneMode.Single);
        var results = new List<object>();
        foreach (string name in new[] { "Dim inspection lamp", "Corridor lamp - warm ivory", "Ward lamp - muted cyan" })
        {
            GameObject root = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).Select(item => item.gameObject).Single();
            var modifications = PrefabUtility.GetPropertyModifications(root).ToList();
            if (name == "Dim inspection lamp")
            {
                modifications.RemoveAll(item => item.propertyPath == "m_ShadowsEnabled");
            }
            else
            {
                foreach (var item in modifications)
                {
                    if (item.propertyPath == "m_Enabled" || item.propertyPath == "m_Intensity")
                    {
                        item.value = "0";
                    }
                    if (item.propertyPath == "m_ShadowsEnabled")
                    {
                        item.value = "1";
                    }
                }
            }
            PrefabUtility.SetPropertyModifications(root, modifications.ToArray());
            if (name != "Dim inspection lamp")
            {
                Light2D light = root.GetComponentsInChildren<Light2D>(true).Single();
                light.enabled = false;
                light.intensity = 0;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            }
            results.Add(new { root = name, overrideCount = modifications.Count });
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Docs/Validation/DeathScreenD01/serialized-override-restoration.json",
            JsonConvert.SerializeObject(results, Formatting.Indented));
        return results;
    }
}
