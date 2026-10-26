using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class InspectStalkerSpriteStates
{
    public static string Read()
    {
        const string folder = "Concepts/Used/Stalker/Locomotion03Sprites";
        Directory.CreateDirectory(folder);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        var animator = new SerializedObject(prefab.GetComponent<StalkerSpriteAnimator>());
        var groups = new Dictionary<string, object>();
        foreach (string field in new[] { "walkFrames", "recoilFrames", "attackFrames", "deathFrames", "eyeFrames" })
        {
            var frames = new List<object>();
            SerializedProperty array = animator.FindProperty(field);
            for (int index = 0; index < array.arraySize; index++)
            {
                Sprite sprite = array.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
                frames.Add(new { index, sprite.name, texture = AssetDatabase.GetAssetPath(sprite.texture),
                    width = sprite.texture.width, height = sprite.texture.height,
                    rect = new[] { sprite.rect.x, sprite.rect.y, sprite.rect.width, sprite.rect.height },
                    pivot = new[] { sprite.pivot.x, sprite.pivot.y }, sprite.pixelsPerUnit });
            }
            groups[field] = frames;
        }
        var states = Resources.FindObjectsOfTypeAll<ShadowStalker2D>()
            .Where(actor => actor.gameObject.scene.IsValid() && actor.gameObject.activeInHierarchy)
            .Select(actor => new { actor.name, state = actor.State.ToString(),
                frame = actor.GetComponent<StalkerSpriteAnimator>().CurrentFrame }).ToArray();
        string json = JsonConvert.SerializeObject(new { playing = EditorApplication.isPlaying, groups, liveStates = states }, Formatting.Indented);
        File.WriteAllText(folder + "/inspection-before.json", json);
        return "Read sprite metadata and " + states.Length + " live actors; scene mutations: zero.";
    }
}
