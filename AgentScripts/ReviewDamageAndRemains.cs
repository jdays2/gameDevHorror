using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviewDamageAndRemains
{
    public static object CaptureLoadedTexture()
    {
        const string path = "Assets/EmergencyShift/Art/Enemies/Stalker/Remains04/HollowCore.png";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;
            readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readable.Apply();
            const string output = "work/damage-remains-20261022/loaded-core.png";
            File.WriteAllBytes(output, readable.EncodeToPNG());
            return new { path, output, texture.width, texture.height };
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            Object.DestroyImmediate(readable);
        }
    }

    public static object Inspect()
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .Select(scene => new { scene.path, scene.isDirty, scene.isLoaded }).ToArray();
        var corpses = Resources.FindObjectsOfTypeAll<EmergencyShift.RemainsRecord2D>()
            .Where(record => record.gameObject.scene.IsValid()).Select(record => new
            {
                record.name,
                parts = record.GetComponentsInChildren<SpriteRenderer>(true).Select(renderer => SpriteInfo(renderer.sprite)).ToArray()
            }).ToArray();
        string[] actors = { "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab", "Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab" };
        var templates = actors.Select(path => new
        {
            path,
            fragments = Fragments(path)
        }).ToArray();
        var result = new { Application.isPlaying, scenes, corpses, templates };
        Directory.CreateDirectory("work/damage-remains-20261022");
        File.WriteAllText("work/damage-remains-20261022/native-before.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static object[] Fragments(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            return new object[] { "Unknown prefab path" };
        }
        var burst = prefab.GetComponentInChildren<EmergencyShift.GoreBurst2D>(true);
        using (var serialized = new SerializedObject(burst))
        {
            var fragments = serialized.FindProperty("fragments");
            return Enumerable.Range(0, fragments.arraySize)
                .Select(index => SpriteInfo(fragments.GetArrayElementAtIndex(index).objectReferenceValue as Sprite)).ToArray();
        }
    }

    private static object SpriteInfo(Sprite sprite)
    {
        if (sprite == null)
        {
            return null;
        }
        return new { sprite.name, asset = AssetDatabase.GetAssetPath(sprite), texture = AssetDatabase.GetAssetPath(sprite.texture),
            dimensions = new[] { sprite.texture.width, sprite.texture.height },
            rect = new[] { sprite.rect.x, sprite.rect.y, sprite.rect.width, sprite.rect.height }, sprite.pixelsPerUnit };
    }
}
