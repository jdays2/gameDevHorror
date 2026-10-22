using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit BUG-006 migration, never invoked automatically on import or Play.
public static class RestoreCreatureArtwork
{
    private const string Evidence = "work/creature-restore-20261022/";
    private const string Sources = "Concepts/Used/Visual/CreatureRestoration01/";

    public static object Inspect()
    {
        return InspectAt("native-before.json");
    }

    public static object InspectAfter()
    {
        return InspectAt("native-after.json");
    }

    private static object InspectAt(string filename)
    {
        RequireEditMode();
        Directory.CreateDirectory(Evidence);
        var scenes = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt)
            .Select(scene => new
            {
                scene.path,
                scene.isDirty,
                objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(transform => new
                    {
                        id = GlobalObjectId.GetGlobalObjectIdSlow(transform).ToString(),
                        name = transform.name,
                        position = transform.position,
                        rotation = transform.rotation,
                        scale = transform.lossyScale,
                        prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject)
                    }).ToArray()
            }).ToArray();
        string[] prefabs =
        {
            "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab",
            "Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab",
            "Assets/EmergencyShift/Prefabs/Environment/RoomFinish01/MorgueFloorDrain.prefab"
        };
        var result = new
        {
            scenes,
            prefabs = prefabs.Select(path => new
            {
                path,
                guid = AssetDatabase.AssetPathToGUID(path),
                objects = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Transform>(true)
                    .Select(transform => new
                    {
                        name = transform.name,
                        components = transform.GetComponents<Component>().Select(component => new
                        {
                            type = component.GetType().FullName,
                            json = EditorJsonUtility.ToJson(component)
                        }).ToArray()
                    }).ToArray()
            }).ToArray()
        };
        File.WriteAllText(Evidence + filename, JsonConvert.SerializeObject(result, Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        return new { sceneCount = scenes.Length, prefabCount = prefabs.Length, evidence = Evidence + filename };
    }

    public static object Restore()
    {
        RequireEditMode();
        Directory.CreateDirectory(Evidence);
        var images = JArray.Parse(File.ReadAllText(Sources + "images.json"));
        var sprites = JArray.Parse(File.ReadAllText(Sources + "native-sprites.json"));
        // Validate the entire input before overwriting any pixels.
        foreach (var row in images)
        {
            string path = (string)row["path"];
            if (AssetDatabase.AssetPathToGUID(path) != (string)row["guid"])
            {
                throw new InvalidOperationException("Texture GUID changed: " + path);
            }
            if (!File.Exists((string)row["source"]))
            {
                throw new FileNotFoundException((string)row["source"]);
            }
        }
        foreach (var row in images)
        {
            string path = (string)row["path"];
            File.Copy((string)row["source"], path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = (int)row["maxSize"];
            importer.SaveAndReimport();
        }
        foreach (var row in sprites)
        {
            string path = (string)row["path"];
            if (AssetDatabase.AssetPathToGUID(path) != (string)row["guid"])
            {
                throw new InvalidOperationException("Sprite GUID changed: " + path);
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>((string)row["texture"]);
            float scaleX = texture.width / (float)row["textureSize"][0];
            float scaleY = texture.height / (float)row["textureSize"][1];
            var rect = new Rect((float)row["rect"][0] * scaleX, (float)row["rect"][1] * scaleY,
                (float)row["rect"][2] * scaleX, (float)row["rect"][3] * scaleY);
            var pivot = new Vector2((float)row["pivot"][0], (float)row["pivot"][1]);
            float ppu = (float)row["ppu"] * (scaleX + scaleY) * 0.5f;
            var current = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var replacement = Sprite.Create(texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
            replacement.name = current.name;
            EditorUtility.CopySerialized(replacement, current);
            UnityEngine.Object.DestroyImmediate(replacement);
            EditorUtility.SetDirty(current);
            AssetDatabase.SaveAssetIfDirty(current);
        }
        var result = new { images = images.Count, sprites = sprites.Count, scenesSaved = 0 };
        File.WriteAllText(Evidence + "restore-result.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    public static object VerifyLoadedReferences()
    {
        RequireEditMode();
        int components = 0;
        int references = 0;
        foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
        {
            foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)))
            {
                if (component == null)
                {
                    throw new InvalidOperationException(scene.path + ": missing component.");
                }
                components++;
                using (var serialized = new SerializedObject(component))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                        {
                            continue;
                        }
                        references++;
                        if (property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default)
                        {
                            throw new InvalidOperationException(component.name + ": unresolved " + property.propertyPath);
                        }
                    }
                }
            }
        }
        var result = new { components, references, missingScripts = 0, unresolvedReferences = 0 };
        File.WriteAllText(Evidence + "loaded-references.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static void RequireEditMode()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before changing art assets.");
        }
    }
}
