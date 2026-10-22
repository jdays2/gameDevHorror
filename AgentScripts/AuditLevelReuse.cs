using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuditLevelReuse
{
    private const string Evidence = "work/level-reuse-20261022/";

    public static object AllScenes()
    {
        return Scan(Evidence);
    }

    public static object AfterScenes()
    {
        return Scan(Evidence + "after/");
    }

    private static object Scan(string directory)
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before authoring audit.");
        }
        Directory.CreateDirectory(directory);
        var results = new List<object>();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/EmergencyShift/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool temporary = !scene.IsValid() || !scene.isLoaded;
            if (temporary)
            {
                scene = EditorSceneManager.OpenPreviewScene(path);
            }
            try
            {
                var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var rows = objects.Select(transform =>
                {
                    GameObject gameObject = transform.gameObject;
                    var renderer = gameObject.GetComponent<SpriteRenderer>();
                    var collider = gameObject.GetComponent<Collider2D>();
                    GameObject nearest = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
                    return new
                    {
                        id = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString(),
                        hierarchy = Hierarchy(transform),
                        gameObject.name,
                        parent = transform.parent == null ? null : Hierarchy(transform.parent),
                        active = gameObject.activeSelf,
                        position = new[] { transform.position.x, transform.position.y, transform.position.z },
                        scale = new[] { transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z },
                        prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject),
                        prefabRoot = nearest == gameObject,
                        components = gameObject.GetComponents<Component>().Select(component => component == null ? "MISSING" : component.GetType().Name).ToArray(),
                        sprite = renderer == null ? null : AssetDatabase.GetAssetPath(renderer.sprite),
                        material = renderer == null ? null : AssetDatabase.GetAssetPath(renderer.sharedMaterial),
                        bounds = renderer == null ? null : new[] { renderer.bounds.center.x, renderer.bounds.center.y, renderer.bounds.size.x, renderer.bounds.size.y },
                        collider = collider == null ? null : new[] { collider.bounds.center.x, collider.bounds.center.y, collider.bounds.size.x, collider.bounds.size.y }
                    };
                }).ToArray();
                var entities = objects.Select(transform => transform.GetComponent<EmergencyShift.PersistentEntity2D>())
                    .Where(entity => entity != null).ToArray();
                var invalidIds = entities.Where(entity => string.IsNullOrWhiteSpace(entity.Id)).Select(entity => entity.name).ToArray();
                var duplicateIds = entities.GroupBy(entity => entity.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
                var result = new { scene = path, scene.isDirty, roots = scene.GetRootGameObjects().Select(root => root.name).ToArray(),
                    persistentEntities = entities.Length, invalidIds, duplicateIds, objects = rows };
                string output = directory + scene.name + "-inventory.json";
                File.WriteAllText(output, JsonConvert.SerializeObject(result, Formatting.Indented));
                results.Add(new { scene = path, objects = rows.Length, roots = result.roots.Length,
                    prefabRoots = rows.Count(row => row.prefabRoot),
                    looseSprites = rows.Count(row => row.sprite != null && string.IsNullOrEmpty(row.prefab)), output });
            }
            finally
            {
                if (temporary)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }
        File.WriteAllText(directory + "inventory-summary.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        return results;
    }

    private static string Hierarchy(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
