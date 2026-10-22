using System;
using System.IO;
using EmergencyShift;
using EmergencyShift.Features.DamageFeedback.Config;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// Explicit targeted profile update; never runs on import or Play.
public static class ApplyDamageFeedbackReview
{
    public static object Apply()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before editing feedback profiles.");
        }
        const string damagePath = "Assets/EmergencyShift/Data/UI/Feedback/DirectionalDamage01.asset";
        var style = AssetDatabase.LoadAssetAtPath<DirectionalDamageStyle>(damagePath);
        string before = EditorJsonUtility.ToJson(style);
        using (var serialized = new SerializedObject(style))
        {
            serialized.FindProperty("bloodColor").colorValue = new Color(.8f, .025f, .04f, 1);
            serialized.FindProperty("holdSeconds").floatValue = .28f;
            serialized.FindProperty("fadeSeconds").floatValue = 1.8f;
            serialized.FindProperty("opacity").floatValue = .9f;
            serialized.FindProperty("depth").floatValue = .19f;
            serialized.FindProperty("spread").floatValue = .62f;
            serialized.FindProperty("cameraDistance").floatValue = .08f;
            serialized.FindProperty("cameraSeconds").floatValue = .22f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            style.CreateSettings();
            AssetDatabase.SaveAssetIfDirty(style);
        }
        var goreChanges = new System.Collections.Generic.List<object>();
        foreach (string name in new[] { "StalkerRupture", "SleeperRupture" })
        {
            string path = "Assets/EmergencyShift/Data/Gore/" + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<GoreProfile>(path);
            string original = EditorJsonUtility.ToJson(profile);
            using (var serialized = new SerializedObject(profile))
            {
                serialized.FindProperty("settledTint").colorValue = new Color(.78f, .73f, .76f, 1);
                serialized.FindProperty("airborneTint").colorValue = new Color(.88f, .82f, .86f, 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                profile.Validate();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            goreChanges.Add(new { path, before = original, after = EditorJsonUtility.ToJson(profile) });
        }
        var result = new { damagePath, before, after = EditorJsonUtility.ToJson(style), goreChanges, scenesSaved = 0 };
        Directory.CreateDirectory("work/damage-remains-20261022");
        File.WriteAllText("work/damage-remains-20261022/profile-update.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }
}
