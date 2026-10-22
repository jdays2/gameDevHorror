using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit native authoring/review for TASK-LEVEL-DANGER. Never regenerates the level.
public static class AuthorEntranceBlood
{
    private const string Output = "work/entrance-traces-20261022/";
    private const string Prefabs = "Assets/EmergencyShift/Prefabs/Environment/BloodTraces05/";

    [Serializable]
    public sealed class Placement
    {
        public string parent;
        public string name;
        public string prefab;
        public float x;
        public float y;
        public float width;
        public float angle;
        public float[] tint;
    }

    private static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    private static float[] C(Color value) => new[] { value.r, value.g, value.b, value.a };

    public static string PathOf(Transform transform)
    {
        return transform.parent == null ? transform.name : PathOf(transform.parent) + "/" + transform.name;
    }

    public static object Inspect()
    {
        Scene scene = SceneManager.GetActiveScene();
        var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var doors = transforms.SelectMany(transform => transform.GetComponents<MonoBehaviour>())
            .Where(component => component != null && component.GetType().Name == "Door2D")
            .Select(component => new { path = PathOf(component.transform), position = V(component.transform.position),
                fields = EditorJsonUtility.ToJson(component),
                sprites = component.GetComponentsInChildren<SpriteRenderer>(true).Select(renderer => new {
                    name = renderer.name, position = V(renderer.transform.position), size = V(renderer.bounds.size) }).ToArray() }).ToArray();
        var enemies = transforms.SelectMany(transform => transform.GetComponents<MonoBehaviour>())
            .Where(component => component != null && component.GetType().Name == "EnemyHealth2D")
            .Select(component => new { path = PathOf(component.transform), type = component.GetType().Name,
                active = component.gameObject.activeInHierarchy, enabled = component.enabled, position = V(component.transform.position) }).ToArray();
        var traces = transforms.SelectMany(transform => transform.GetComponents<SpriteRenderer>())
            .Where(renderer => renderer.sprite != null && AssetDatabase.GetAssetPath(renderer.sprite).Contains("Blood"))
            .Select(renderer => new { path = PathOf(renderer.transform), position = V(renderer.transform.position),
                size = V(renderer.bounds.size), color = C(renderer.color),
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renderer.gameObject) }).ToArray();
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "inventory-before.json", JsonConvert.SerializeObject(new { scene = scene.path, dirty = scene.isDirty,
            roots = scene.GetRootGameObjects().Select(root => root.name).ToArray(), doors, enemies, traces }, Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        return new { scene = scene.path, dirty = scene.isDirty, doors = doors.Length, enemies = enemies.Length, traces = traces.Length };
    }

    public static object Apply(string manifest)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || !scene.path.EndsWith("/MorgueRouteTest.unity"))
        {
            throw new InvalidOperationException("Requires the clean authored MorgueRouteTest scene in EditMode.");
        }
        var placements = JsonConvert.DeserializeObject<Placement[]>(File.ReadAllText(manifest));
        var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .GroupBy(transform => PathOf(transform)).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
        // Validate the entire recipe before creating anything. Never update existing objects by guessed names.
        foreach (var placement in placements)
        {
            if (!transforms.ContainsKey(placement.parent) || transforms[placement.parent].lossyScale != Vector3.one
                || transforms.ContainsKey(placement.parent + "/" + placement.name)
                || placement.width <= 0 || placement.tint == null || placement.tint.Length != 4)
            {
                throw new InvalidOperationException("Invalid or already applied placement: " + placement.name);
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + placement.prefab + ".prefab");
            if (prefab == null || prefab.GetComponentsInChildren<SpriteRenderer>(true).Length != 1
                || prefab.GetComponent<SpriteRenderer>() == null || prefab.GetComponent<SpriteRenderer>().sprite == null
                || prefab.GetComponentsInChildren<Component>(true).Any(component => !(component is Transform) && !(component is SpriteRenderer)))
            {
                throw new InvalidOperationException("Decoration must be an existing single-renderer prefab: " + placement.prefab);
            }
        }
        File.Copy(scene.path, Output + "MorgueRouteTest-before.unity", false);
        var guard = new LevelAuthoringState(scene);
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Author local entrance blood");
        try
        {
            foreach (var placement in placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + placement.prefab + ".prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(instance, "Add entrance blood overlay");
                instance.name = placement.name;
                instance.transform.SetParent(transforms[placement.parent], true);
                instance.transform.position = new Vector3(placement.x, placement.y, 0);
                instance.transform.rotation = Quaternion.Euler(0, 0, placement.angle);
                var renderer = instance.GetComponent<SpriteRenderer>();
                float scale = placement.width / renderer.sprite.bounds.size.x;
                instance.transform.localScale = Vector3.one * scale;
                renderer.color = new Color(placement.tint[0], placement.tint[1], placement.tint[2], placement.tint[3]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            object preserved = guard.AssertPreserved();
            object references = VerifyReferences(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new IOException("Unable to save entrance decoration.");
            }
            var result = new { scene = scene.path, overlays = placements.Length, preserved, references, scene.isDirty };
            File.WriteAllText(Output + "authoring.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            Undo.CollapseUndoOperations(undo);
            return result;
        }
        catch
        {
            Undo.RevertAllDownToGroup(undo);
            throw;
        }
    }

    private static object VerifyReferences(Scene scene)
    {
        int checkedObjects = 0;
        foreach (var transform in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
            {
                throw new InvalidOperationException("Missing script: " + PathOf(transform));
            }
            foreach (var component in transform.GetComponents<Component>())
            {
                using (var serialized = new SerializedObject(component))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference
                            && property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default)
                        {
                            throw new InvalidOperationException("Missing reference: " + PathOf(transform) + "." + property.propertyPath);
                        }
                    }
                }
                checkedObjects++;
            }
        }
        return new { checkedObjects, missingScripts = 0, brokenReferences = 0 };
    }

    public static object Verify(string manifest)
    {
        Scene scene = SceneManager.GetActiveScene();
        var placements = JsonConvert.DeserializeObject<Placement[]>(File.ReadAllText(manifest));
        var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var obstacles = transforms.SelectMany(transform => transform.GetComponents<Collider2D>())
            .Where(collider => collider.enabled && !collider.isTrigger && collider.GetComponentInParent<EmergencyShift.Door2D>() == null
                && collider.GetComponentInParent<EmergencyShift.EnemyHealth2D>() == null
                && collider.GetComponentInParent<EmergencyShift.PlayerLife2D>() == null).ToArray();
        foreach (var placement in placements)
        {
            var transform = transforms.Single(value => PathOf(value) == placement.parent + "/" + placement.name);
            var renderer = transform.GetComponent<SpriteRenderer>();
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) != Prefabs + placement.prefab + ".prefab"
                || renderer.sprite == null || renderer.sharedMaterial == null || renderer.sortingOrder != -3
                || transform.GetComponentsInChildren<Component>(true).Any(component => !(component is Transform) && !(component is SpriteRenderer)))
            {
                throw new InvalidOperationException("Invalid blood overlay: " + placement.name);
            }
            foreach (var obstacle in obstacles)
            {
                if (renderer.bounds.Intersects(obstacle.bounds))
                {
                    throw new InvalidOperationException("Overlay bounds overlap solid geometry: " + placement.name + " / " + PathOf(obstacle.transform));
                }
            }
        }
        var result = new { scene = scene.path, overlays = placements.Length, geometryOverlaps = 0,
            colliders = 0, scripts = 0, shadows = 0, references = VerifyReferences(scene), scene.isDirty };
        File.WriteAllText(Output + "verification.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }
}
