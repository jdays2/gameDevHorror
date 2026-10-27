using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Remove the rejected 05 trial assignment without touching authored scenes.</summary>
public static class RestoreStalkerPublishedFrames05
{
    public static string Restore()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires stopped Editor.");
        }
        JObject manifest = JObject.Parse(File.ReadAllText("Concepts/Used/Stalker/Postures04/sprites.json"));
        var sprites = new Dictionary<string, Sprite>();
        foreach (JToken atlas in manifest["atlases"])
        {
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath((string)atlas["asset"]).OfType<Sprite>())
            {
                sprites.Add(sprite.name, sprite);
            }
        }
        var set = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
        var serialized = new SerializedObject(set);
        SerializedProperty frames = serialized.FindProperty("frames");
        for (int row = 0; row < 9; row++)
        {
            for (int phase = 0; phase < 48; phase++)
            {
                frames.GetArrayElementAtIndex(row * 48 + phase).objectReferenceValue = sprites["body-" + row + "-" + phase.ToString("D2")];
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        set.Validate();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssetIfDirty(set);
        const string path = "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var animator = new SerializedObject(prefab.GetComponent<StalkerSpriteAnimator>());
            SerializedProperty walk = animator.FindProperty("walkFrames");
            for (int phase = 0; phase < 48; phase++)
            {
                walk.GetArrayElementAtIndex(phase).objectReferenceValue = set.Frame(phase);
            }
            animator.ApplyModifiedPropertiesWithoutUndo();
            prefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sprite = set.Frame(0);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        return "Previous frame assignments restored; rejected 05 atlas inactive; no scene saves.";
    }
}
