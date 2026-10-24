using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only evidence for TASK-001. Never saves or changes authored scene objects.
public static class InspectTask001
{
    private const string Output = "Docs/Validation/Task001LevelDesign/";

    public static object Capture()
    {
        return CaptureScene(SceneManager.GetActiveScene(), Application.isPlaying ? "runtime-scene.json" : "authored-scene.json");
    }

    public static object CaptureSavedSandbox()
    {
        if (Application.isPlaying)
        {
            return new { skipped = true, reason = "Editor is occupied by Play Mode; no scene change attempted." };
        }

        var preview = EditorSceneManager.OpenPreviewScene("Assets/EmergencyShift/Scenes/MechanicsSandbox.unity");
        try
        {
            return CaptureScene(preview, "sandbox-saved.json");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static object CaptureScene(Scene scene, string filename)
    {
        Directory.CreateDirectory(Output);
        var transforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var assets = new Dictionary<string, object>();
        var objects = new List<object>();
        foreach (var transform in transforms)
        {
            var components = transform.GetComponents<Component>();
            var selected = components.Where(component => component != null && Relevant(component.GetType().Name)).ToArray();
            var colliders = components.OfType<Collider2D>().ToArray();
            var sprite = transform.GetComponent<SpriteRenderer>();
            bool floor = sprite != null && transform.name.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0;
            if (selected.Length == 0 && colliders.Length == 0 && !floor)
            {
                continue;
            }

            var serialized = selected.Select(component => Describe(component, assets)).ToArray();
            objects.Add(new
            {
                path = PathOf(transform),
                position = Vector(transform.position),
                scale = Vector(transform.lossyScale),
                rotation = transform.eulerAngles.z,
                active = transform.gameObject.activeInHierarchy,
                components = serialized,
                colliders = colliders.Select(collider => new
                {
                    type = collider.GetType().Name,
                    collider.enabled,
                    collider.isTrigger,
                    layer = LayerMask.LayerToName(collider.gameObject.layer),
                    data = JObject.Parse(EditorJsonUtility.ToJson(collider)),
                    center = Vector(collider.bounds.center),
                    size = Vector(collider.bounds.size)
                }).ToArray(),
                floor = floor ? new { size = Vector(sprite.bounds.size), center = Vector(sprite.bounds.center) } : null
            });
        }

        var result = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            source = EditorSceneManager.IsPreviewScene(scene) ? "Saved scene opened read-only in a temporary preview scene" : "Actual in-memory Unity scene, including unsaved owner changes",
            scene = scene.path,
            scene.isDirty,
            playing = Application.isPlaying,
            frames = Time.frameCount,
            roots = scene.GetRootGameObjects().Select(root => root.name).ToArray(),
            objects,
            assets
        };
        string file = Output + filename;
        File.WriteAllText(file, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { file, scene = scene.path, scene.isDirty, playing = Application.isPlaying, objects = objects.Count, assets = assets.Count, sceneSaved = false };
    }

    private static object Describe(Component component, Dictionary<string, object> assets)
    {
        var references = new List<object>();
        using (var serialized = new SerializedObject(component))
        {
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                {
                    continue;
                }

                var value = property.objectReferenceValue;
                string path = AssetDatabase.GetAssetPath(value);
                references.Add(new { field = property.propertyPath, name = value.name, type = value.GetType().Name, asset = path });
                if (value is ScriptableObject && !string.IsNullOrEmpty(path) && !assets.ContainsKey(path))
                {
                    assets[path] = JObject.Parse(EditorJsonUtility.ToJson(value));
                }
            }
        }

        return new { type = component.GetType().Name, data = JObject.Parse(EditorJsonUtility.ToJson(component)), references };
    }

    private static bool Relevant(string name)
    {
        return name.StartsWith("Door", StringComparison.Ordinal) || name.StartsWith("Player", StringComparison.Ordinal)
            || name.StartsWith("Sandbox", StringComparison.Ordinal) || name.Contains("Pickup")
            || name == "ShadowStalker2D" || name == "Sleeper2D" || name == "EnemyPatrol2D" || name == "Flashlight2D"
            || name == "FlashlightUpgrades" || name == "LevelContext2D" || name == "Camera";
    }

    private static string PathOf(Transform transform)
    {
        return transform.parent == null ? transform.name : PathOf(transform.parent) + "/" + transform.name;
    }

    private static float[] Vector(Vector3 value) => new[] { value.x, value.y, value.z };
}
