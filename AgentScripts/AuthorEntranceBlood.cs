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

    private static string PrefabPath(Placement placement)
    {
        if (placement.prefab.StartsWith("Assets/EmergencyShift/Prefabs/Environment/", StringComparison.Ordinal)
            && placement.prefab.EndsWith(".prefab", StringComparison.Ordinal) && !placement.prefab.Contains(".."))
        {
            return placement.prefab;
        }
        return Prefabs + placement.prefab + ".prefab";
    }

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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(placement));
            if (prefab == null || prefab.GetComponentsInChildren<SpriteRenderer>(true).Length != 1
                || prefab.GetComponent<SpriteRenderer>() == null || prefab.GetComponent<SpriteRenderer>().sprite == null
                || prefab.GetComponentsInChildren<Component>(true).Any(component => !(component is Transform) && !(component is SpriteRenderer)))
            {
                throw new InvalidOperationException("Decoration must be an existing single-renderer prefab: " + placement.prefab);
            }
        }
        string evidence = Output + Path.GetFileNameWithoutExtension(manifest) + "/";
        Directory.CreateDirectory(evidence);
        File.Copy(scene.path, evidence + "MorgueRouteTest-before.unity", false);
        var guard = new LevelAuthoringState(scene);
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Author local entrance blood");
        try
        {
            foreach (var placement in placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(placement));
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
            File.WriteAllText(evidence + "authoring.json", JsonConvert.SerializeObject(result, Formatting.Indented));
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
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) != PrefabPath(placement)
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

    // Follow-up to the owner's annotated screenshot: dense blood centred under the whole leaf.
    public static object RefinePools(string manifest)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.isDirty || !scene.path.EndsWith("/MorgueRouteTest.unity"))
        {
            throw new InvalidOperationException("Clean authored scene in EditMode required.");
        }
        var placements = JsonConvert.DeserializeObject<Placement[]>(File.ReadAllText(manifest));
        var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var instances = placements.Select(placement => transforms.Single(transform => PathOf(transform) == placement.parent + "/" + placement.name)).ToArray();
        var ownedOwners = instances.SelectMany(transform => new UnityEngine.Object[] { transform.gameObject, transform, transform.GetComponent<SpriteRenderer>() }).ToArray();
        var originalIds = ownedOwners.Select(owner => GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString()).ToArray();
        var ownedIds = originalIds.Select(id => id + "|").ToArray();
        var before = LevelAuthoringState.SavedFields(scene).Where(pair => !ownedIds.Any(id => pair.Key.StartsWith(id, StringComparison.Ordinal)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (string form in new[] { "Fresh", "Clotted" })
            {
                string spritePath = "Assets/EmergencyShift/Art/Effects/BloodPools01/" + form + "StaticCore.asset";
                string prefabPath = "Assets/EmergencyShift/Prefabs/Environment/LevelSurfaces/BloodDecal-" + form + "-Dense.prefab";
                if (AssetDatabase.LoadMainAssetAtPath(spritePath) != null || AssetDatabase.LoadMainAssetAtPath(prefabPath) != null)
                {
                    throw new InvalidOperationException("This explicit authoring revision was already applied.");
                }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EmergencyShift/Art/Effects/BloodPools01/" + form + ".png");
                // Read-only alpha analysis identified the connected main mass; retain a transparent border.
                Rect rect = form == "Fresh" ? new Rect(191, 160, 246, 297) : new Rect(197, 125, 274, 346);
                var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), rect.width, 0, SpriteMeshType.FullRect);
                sprite.name = form + "StaticCore";
                AssetDatabase.CreateAsset(sprite, spritePath);
                var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Environment/LevelSurfaces/BloodDecal-" + form + "-Simple.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(original, preview);
                instance.GetComponent<SpriteRenderer>().sprite = sprite;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<SpriteRenderer>());
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            for (int index = 0; index < placements.Length; index++)
            {
                var placement = placements[index];
                var instance = instances[index].gameObject;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(placement));
                PrefabUtility.ReplacePrefabAssetOfPrefabInstance(instance, prefab, InteractionMode.AutomatedAction);
                instance.name = placement.name;
                instance.transform.position = new Vector3(placement.x, placement.y, 0);
                instance.transform.rotation = Quaternion.Euler(0, 0, placement.angle);
                instance.transform.localScale = Vector3.one * placement.width;
                var renderer = instance.GetComponent<SpriteRenderer>();
                renderer.color = new Color(placement.tint[0], placement.tint[1], placement.tint[2], placement.tint[3]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            var after = LevelAuthoringState.SavedFields(scene);
            var aliases = originalIds.Select((id, index) => new { id, current = GlobalObjectId.GetGlobalObjectIdSlow(ownedOwners[index]).ToString() })
                .ToDictionary(pair => pair.id, pair => pair.current);
            if (before.Any(pair => !after.TryGetValue(pair.Key, out string value)
                || value != pair.Value && (!aliases.TryGetValue(pair.Value, out string alias) || value != alias)))
            {
                throw new InvalidOperationException("An object outside the 18 owned pool layers changed; do not save.");
            }
            object references = VerifyReferences(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return new { ownedPoolLayers = instances.Length, preservedOtherFields = before.Count, unchanged = true, references };
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    public static object SaveVerifiedRefinement(string manifest)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("EditMode required.");
        }
        var names = JsonConvert.DeserializeObject<Placement[]>(File.ReadAllText(manifest)).Select(placement => placement.parent + "/" + placement.name).ToArray();
        Scene saved = EditorSceneManager.OpenPreviewScene(scene.path);
        try
        {
            var memoryTransforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var savedTransforms = saved.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var aliases = new System.Collections.Generic.Dictionary<string, string>();
            foreach (string name in names)
            {
                var oldTransform = savedTransforms.Single(transform => PathOf(transform) == name);
                var newTransform = memoryTransforms.Single(transform => PathOf(transform) == name);
                var oldOwners = new UnityEngine.Object[] { oldTransform.gameObject, oldTransform, oldTransform.GetComponent<SpriteRenderer>() };
                var newOwners = new UnityEngine.Object[] { newTransform.gameObject, newTransform, newTransform.GetComponent<SpriteRenderer>() };
                for (int index = 0; index < oldOwners.Length; index++)
                {
                    aliases[GlobalObjectId.GetGlobalObjectIdSlow(oldOwners[index]).ToString()] = GlobalObjectId.GetGlobalObjectIdSlow(newOwners[index]).ToString();
                }
            }
            var before = LevelAuthoringState.SavedFields(saved);
            var after = LevelAuthoringState.SavedFields(scene);
            int preserved = 0;
            foreach (var pair in before)
            {
                if (aliases.Keys.Any(id => pair.Key.StartsWith(id + "|", StringComparison.Ordinal)))
                {
                    continue;
                }
                if (!after.TryGetValue(pair.Key, out string value) || value != pair.Value
                    && (!aliases.TryGetValue(pair.Value, out string alias) || value != alias))
                {
                    throw new InvalidOperationException("Unexpected non-pool change: " + pair.Key);
                }
                preserved++;
            }
            object verified = Verify(manifest);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var result = new { ownedPoolLayers = names.Length, unchangedOutsidePools = true, preservedFields = preserved, verified };
            File.WriteAllText("work/entrance-pools-20261022/refinement.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(saved);
        }
    }

    public static object PublishWithoutSourceSave()
    {
        Scene source = SceneManager.GetActiveScene();
        if (Application.isPlaying || source.isDirty || !source.path.EndsWith("/MorgueRouteTest.unity"))
        {
            throw new InvalidOperationException("Publication requires a clean saved Morgue scene.");
        }
        var sourceBytes = File.ReadAllBytes(source.path);
        var before = LevelAuthoringState.SavedFields(source);
        const string copyPath = "Assets/_Recovery/LevelReuse/MorgueRouteTest-pools49-publication.unity";
        if (File.Exists(copyPath) || !AssetDatabase.CopyAsset(source.path, copyPath))
        {
            throw new InvalidOperationException("Cannot create an owned separate asset copy.");
        }
        Scene copy = default;
        try
        {
            copy = EditorSceneManager.OpenScene(copyPath, OpenSceneMode.Additive);
            foreach (var root in copy.GetRootGameObjects().Where(root => root.name == "Pause Menu"))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            object references = VerifyReferences(copy);
            EditorSceneManager.MarkSceneDirty(copy);
            EditorSceneManager.SaveScene(copy);
            File.Copy(copyPath, "work/entrance-pools-20261022/MorgueRouteTest-publication.unity", true);
            var after = LevelAuthoringState.SavedFields(source);
            if (!File.ReadAllBytes(source.path).SequenceEqual(sourceBytes) || before.Count != after.Count
                || before.Any(pair => !after.TryGetValue(pair.Key, out string value) || pair.Value != value))
            {
                throw new InvalidOperationException("Source changed while exporting; do not publish.");
            }
            var result = new { sourceUnchanged = true, sourceFields = before.Count, sourceDirty = source.isDirty, sourceSaved = false, references };
            File.WriteAllText("work/entrance-pools-20261022/publication.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            if (copy.IsValid() && copy.isLoaded && !copy.isDirty)
            {
                EditorSceneManager.CloseScene(copy, true);
                AssetDatabase.DeleteAsset(copyPath);
            }
        }
    }
}
