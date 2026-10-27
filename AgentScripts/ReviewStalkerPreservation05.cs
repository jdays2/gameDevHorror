using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Read-only preservation evidence for every authored Stalker, including inactive hierarchies.</summary>
public static class ReviewStalkerPreservation05
{
    private const string Folder = "Concepts/Used/Stalker/Postures05/";
    private const string BaselinePath = Folder + "scene-components-before.json";
    private const string ReportPath = Folder + "preservation-full.json";
    private static readonly string[] Scenes =
    {
        "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity",
        "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity",
        "Assets/EmergencyShift/Scenes/BasementPrototype.unity"
    };

    public static object CaptureBaseline()
    {
        RequireStopped();
        if (File.Exists(BaselinePath))
        {
            throw new InvalidOperationException("Preservation baseline already exists; it must not be overwritten: " + BaselinePath);
        }

        JObject snapshot = ReadAll(false);
        Directory.CreateDirectory(Folder);
        using (var stream = new FileStream(BaselinePath, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(snapshot.ToString());
        }
        JObject report = Summary(snapshot, "Baseline captured before presentation import; Verify remains pending.");
        report["baselineSha256"] = HashFile(BaselinePath);
        File.WriteAllText(ReportPath, report.ToString());
        return Compact(report);
    }

    public static object Verify() => VerifyCore(true);

    public static object VerifyFieldsOnly() => VerifyCore(false);

    public static object InspectFinalDifferences()
    {
        RequireStopped();
        JObject before = JObject.Parse(File.ReadAllText(BaselinePath));
        JObject current = JObject.Parse(ReadAll(true).ToString());
        var differences = new JArray();
        foreach (JObject prior in before["actors"])
        {
            JObject actor = current["actors"].OfType<JObject>().Single(value =>
                (string)value["scene"] == (string)prior["scene"] &&
                (string)value["identity"]["globalId"] == (string)prior["identity"]["globalId"]);
            foreach (string key in new[] { "id", "identity", "animatorIdentity", "hierarchy", "position", "rotation", "scale", "prefab", "prefabGuid", "objects" })
            {
                if (!JToken.DeepEquals(prior[key], actor[key]))
                {
                    differences.Add(new JObject { ["actor"] = actor["hierarchy"].DeepClone(), ["key"] = key,
                        ["before"] = prior[key]?.DeepClone(), ["after"] = actor[key]?.DeepClone() });
                }
            }
            JObject oldFields = (JObject)prior["fields"];
            JObject newFields = (JObject)actor["fields"];
            foreach (string field in oldFields.Properties().Select(value => value.Name).Union(newFields.Properties().Select(value => value.Name)))
            {
                if (!JToken.DeepEquals(oldFields[field], newFields[field]))
                {
                    string owner = field.Split('|')[0];
                    JToken component = actor["objects"].FirstOrDefault(value => (string)value["identity"]["globalId"] == owner);
                    differences.Add(new JObject { ["actor"] = actor["hierarchy"].DeepClone(), ["key"] = field,
                        ["componentType"] = component?["type"]?.DeepClone(), ["addedAfterBaseline"] = oldFields[field] == null,
                        ["before"] = oldFields[field]?.DeepClone(), ["after"] = newFields[field]?.DeepClone() });
                }
            }
        }
        JObject report = Summary(current, "Read-only final comparison; differences are reported without modifying or replacing the original baseline or prior strict PASS.");
        report["baselineSha256"] = HashFile(BaselinePath);
        report["baselineActorCount"] = before["actors"].Count();
        report["differences"] = differences;
        const string path = Folder + "preservation-final-differences.json";
        File.WriteAllText(path, report.ToString());
        return new { actors = current["actors"].Count(), baselineActors = before["actors"].Count(), differences = differences.Count,
            currentBodyAndAttackSetsVerified = true, sceneSaves = 0, report = path };
    }

    private static object VerifyCore(bool verifySets)
    {
        RequireStopped();
        if (!File.Exists(BaselinePath))
        {
            throw new InvalidOperationException("CaptureBaseline must run before updating the Stalker presentation.");
        }

        JObject before = JObject.Parse(File.ReadAllText(BaselinePath));
        // Match the persisted JSON representation, including null string values.
        JObject current = JObject.Parse(ReadAll(verifySets).ToString());
        JArray priorActors = (JArray)before["actors"];
        JArray currentActors = (JArray)current["actors"];
        if (priorActors.Count != currentActors.Count)
        {
            throw new InvalidOperationException("Authored Stalker count changed across the three scenes.");
        }

        foreach (JObject prior in priorActors)
        {
            JObject actor = currentActors.OfType<JObject>().SingleOrDefault(value =>
                (string)value["scene"] == (string)prior["scene"] &&
                (string)value["identity"]["globalId"] == (string)prior["identity"]["globalId"]);
            if (actor == null)
            {
                throw new InvalidOperationException("Stalker disappeared or its persistent ID changed: " + prior["scene"] + " | " + prior["id"]);
            }
            foreach (string key in new[] { "id", "identity", "animatorIdentity", "hierarchy", "position", "rotation", "scale", "prefab", "prefabGuid", "objects", "fields" })
            {
                if (!JToken.DeepEquals(prior[key], actor[key]))
                {
                    string detail = key;
                    if (key == "fields")
                    {
                        JObject left = (JObject)prior[key];
                        JObject right = (JObject)actor[key];
                        detail = left.Properties().Select(property => property.Name).Union(right.Properties().Select(property => property.Name))
                            .First(name => !JToken.DeepEquals(left[name], right[name]));
                    }
                    throw new InvalidOperationException("Authored Stalker changed: " + actor["hierarchy"] + " | " + detail);
                }
            }
        }

        JObject report = Summary(current, "Verified: all prior actor hierarchy identities and authored component fields preserved.");
        report["baselineSha256"] = HashFile(BaselinePath);
        report["allPriorFieldsPreserved"] = true;
        report["currentBodyAndAttackSetsVerified"] = verifySets;
        File.WriteAllText(ReportPath, report.ToString());
        return Compact(report);
    }

    private static JObject ReadAll(bool verifySets)
    {
        Scene active = SceneManager.GetActiveScene();
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var dirty = new Dictionary<Scene, bool>();
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            dirty[scene] = scene.isDirty;
        }
        var actors = new JArray();
        var scenes = new JArray();
        var assetHashes = new Dictionary<string, string>();
        try
        {
            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasPresent = scene.IsValid();
                bool opened = !wasPresent || !scene.isLoaded;
                if (opened)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
                bool dirtyBefore = scene.isDirty;
                try
                {
                    ShadowStalker2D[] found = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShadowStalker2D>(true))
                        .OrderBy(actor => GlobalObjectId.GetGlobalObjectIdSlow(actor).ToString(), StringComparer.Ordinal).ToArray();
                    var ids = new HashSet<string>();
                    foreach (ShadowStalker2D actor in found)
                    {
                        PersistentEntity2D persistent = actor.GetComponent<PersistentEntity2D>();
                        if (persistent != null && (string.IsNullOrWhiteSpace(persistent.Id) || !ids.Add(persistent.Id)))
                        {
                            throw new InvalidOperationException("Missing or duplicate Stalker persistent ID in " + path + " | " + actor.name);
                        }
                        if (verifySets)
                        {
                            VerifySets(actor);
                        }
                        actors.Add(Snapshot(actor, assetHashes));
                    }
                    if (dirtyBefore != scene.isDirty)
                    {
                        throw new InvalidOperationException("Read-only preservation capture changed a scene dirty flag: " + path);
                    }
                    scenes.Add(new JObject { ["path"] = path, ["actors"] = found.Length, ["dirtyBefore"] = dirtyBefore,
                        ["dirtyAfter"] = scene.isDirty, ["temporarilyOpened"] = opened, ["savedFileSha256"] = HashFile(path) });
                }
                finally
                {
                    if (opened)
                    {
                        EditorSceneManager.CloseScene(scene, !wasPresent);
                    }
                }
            }
        }
        finally
        {
            if (active.IsValid() && active.isLoaded)
            {
                SceneManager.SetActiveScene(active);
            }
            SceneSetup[] after = EditorSceneManager.GetSceneManagerSetup();
            if (setup.Length != after.Length || setup.Where((value, index) => value.path != after[index].path ||
                value.isLoaded != after[index].isLoaded || value.isActive != after[index].isActive).Any())
            {
                throw new InvalidOperationException("Read-only inspection failed to preserve the prior loaded scene setup.");
            }
            foreach (KeyValuePair<Scene, bool> prior in dirty)
            {
                Scene scene = prior.Key;
                if (scene.IsValid() && scene.isDirty != prior.Value)
                {
                    throw new InvalidOperationException("Read-only inspection changed the existing dirty scene state: " + scene.path);
                }
            }
        }
        return new JObject { ["actors"] = actors, ["scenes"] = scenes, ["sceneSaves"] = 0,
            ["priorLoadedAndDirtyStatePreserved"] = true, ["excludedIntendedFields"] = new JArray(
                "Visual/Body SpriteRenderer.m_Sprite", "Animator.spriteSet", "Animator.attackSpriteSet", "Animator.walkFrames", "Animator.locomotionProfile") };
    }

    private static JObject Snapshot(ShadowStalker2D actor, Dictionary<string, string> hashes)
    {
        var fields = new JObject();
        var objects = new JArray();
        var animator = actor.GetComponent<StalkerSpriteAnimator>();
        SpriteRenderer body = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        foreach (Transform child in actor.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
            {
                throw new InvalidOperationException("Missing script in authored Stalker hierarchy: " + Hierarchy(child));
            }
            objects.Add(new JObject { ["identity"] = Identity(child.gameObject), ["type"] = "UnityEngine.GameObject" });
            SnapshotObject(child.gameObject, body, fields, hashes);
            foreach (Component component in child.GetComponents<Component>())
            {
                objects.Add(new JObject { ["identity"] = Identity(component), ["type"] = component.GetType().FullName });
                SnapshotObject(component, body, fields, hashes);
            }
        }
        string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor);
        var persistent = actor.GetComponent<PersistentEntity2D>();
        var rigidbody = actor.GetComponent<Rigidbody2D>();
        var crowd = actor.GetComponent<CrowdAgent2D>();
        var animatorFields = new SerializedObject(animator);
        var variant = new JObject
        {
            ["definition"] = AssetDatabase.GetAssetPath(actor.Definition),
            ["pursuitSpeed"] = actor.Definition.PursuitSpeed,
            ["behaviourProfile"] = AssetDatabase.GetAssetPath(actor.BehaviourProfile),
            ["worldScale"] = ScaleGroup(actor.transform.lossyScale),
            ["visualWorldScale"] = ScaleGroup(actor.transform.Find("Visual").lossyScale),
            ["bodyWorldScale"] = ScaleGroup(body.transform.lossyScale),
            ["bodyTint"] = body.color.ToString("R"),
            ["bodySpriteAsset"] = AssetDatabase.GetAssetPath(body.sprite),
            ["animator"] = animator.GetType().FullName,
            ["locomotionProfile"] = AssetDatabase.GetAssetPath(animatorFields.FindProperty("locomotionProfile").objectReferenceValue),
            ["attacksEnabled"] = actor.AttacksEnabled,
            ["rigidbodyConstraints"] = rigidbody != null ? rigidbody.constraints.ToString() : null,
            ["rigidbodySimulated"] = rigidbody != null && rigidbody.simulated,
            ["crowdEnabled"] = crowd != null && crowd.enabled
        };
        return new JObject { ["scene"] = actor.gameObject.scene.path, ["id"] = persistent != null ? persistent.Id : null,
            ["identity"] = Identity(actor), ["animatorIdentity"] = Identity(animator), ["hierarchy"] = Hierarchy(actor.transform),
            ["position"] = actor.transform.position.ToString("R"), ["rotation"] = actor.transform.rotation.ToString("R"),
            ["scale"] = actor.transform.lossyScale.ToString("R"), ["prefab"] = prefab, ["prefabGuid"] = AssetDatabase.AssetPathToGUID(prefab),
            ["objects"] = objects, ["fields"] = fields, ["presentationVariant"] = variant };
    }

    private static void SnapshotObject(UnityEngine.Object target, SpriteRenderer body, JObject fields, Dictionary<string, string> hashes)
    {
        string owner = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
        using (var serialized = new SerializedObject(target))
        {
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = true;
                string path = property.propertyPath;
                bool animatorField = target is CreatureSpriteAnimator2D &&
                    (path == "spriteSet" || path == "attackSpriteSet" || path == "walkFrames" || path == "locomotionProfile");
                if (animatorField || target == body && path == "m_Sprite" || path.StartsWith("m_Prefab", StringComparison.Ordinal) ||
                    path.StartsWith("m_Corresponding", StringComparison.Ordinal) || path == "m_EditorClassIdentifier")
                {
                    enterChildren = false;
                    continue;
                }
                string value = FieldValue(property, hashes);
                if (value != null)
                {
                    fields[owner + "|" + path] = value;
                }
                // Native reference internals contain temporary m_FileID/m_PathID values.
                // The complete GlobalObjectId above preserves GUID/local file ID instead.
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    enterChildren = false;
                }
            }
        }
    }

    private static string FieldValue(SerializedProperty property, Dictionary<string, string> hashes)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                UnityEngine.Object value = property.objectReferenceValue;
                if (value == null)
                {
                    return property.objectReferenceEntityIdValue != default ? "missing-reference" : "null";
                }
                string identity = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
                string path = AssetDatabase.GetAssetPath(value);
                if (value is ScriptableObject && File.Exists(path))
                {
                    if (!hashes.TryGetValue(path, out string hash))
                    {
                        hash = HashFile(path);
                        hashes.Add(path, hash);
                    }
                    identity += "|sha256:" + hash;
                }
                return identity;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character:
                return property.longValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.Float:
                return property.doubleValue.ToString("R", CultureInfo.InvariantCulture);
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();
            case SerializedPropertyType.String:
                return property.stringValue;
            default:
                return null;
        }
    }

    private static void VerifySets(ShadowStalker2D actor)
    {
        var animator = actor.GetComponent<StalkerSpriteAnimator>();
        if (animator == null || actor.GetComponents<CreatureSpriteAnimator2D>().Length != 1 || animator.SpriteSet == null ||
            AssetDatabase.GetAssetPath(animator.SpriteSet) != "Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset")
        {
            throw new InvalidOperationException("Actual scene actor has a missing or foreign Stalker body set: " + actor.name);
        }
        animator.SpriteSet.Validate();
        var serialized = new SerializedObject(animator);
        var attacks = serialized.FindProperty("attackSpriteSet").objectReferenceValue as CreatureAttackSpriteSet;
        if (attacks == null || AssetDatabase.GetAssetPath(attacks) != "Assets/EmergencyShift/Data/Enemies/StalkerAttackSprites.asset")
        {
            throw new InvalidOperationException("Actual scene actor does not inherit the Stalker attack set: " + actor.name);
        }
        attacks.Validate();
        SpriteRenderer body = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        if (body.sprite == null || !AssetDatabase.GetAssetPath(body.sprite).Contains("/Stalker/Postures05/") ||
            actor.GetComponentsInChildren<MeshRenderer>(true).Length != 0)
        {
            throw new InvalidOperationException("Stalker body is stale or has a runtime limb mesh: " + actor.name);
        }
    }

    private static JObject Identity(UnityEngine.Object value)
    {
        GlobalObjectId identity = GlobalObjectId.GetGlobalObjectIdSlow(value);
        return new JObject { ["globalId"] = identity.ToString(), ["assetGuid"] = identity.assetGUID.ToString(),
            ["fileId"] = identity.targetObjectId.ToString(CultureInfo.InvariantCulture),
            ["prefabInstanceFileId"] = identity.targetPrefabId.ToString(CultureInfo.InvariantCulture) };
    }

    private static JObject Summary(JObject snapshot, string status)
    {
        bool runtimeHasIk = typeof(StalkerSpriteAnimator).Assembly.GetType("EmergencyShift.Features.Enemies.Logic.StalkerGaitModel") != null;
        if (runtimeHasIk)
        {
            throw new InvalidOperationException("Authoring limb logic is compiled into runtime.");
        }
        return new JObject { ["status"] = status, ["count"] = snapshot["actors"].Count(), ["scenes"] = snapshot["scenes"].DeepClone(),
            ["actorIdentities"] = new JArray(snapshot["actors"].Select(row => new JObject { ["scene"] = row["scene"].DeepClone(),
                ["id"] = row["id"].DeepClone(), ["hierarchy"] = row["hierarchy"].DeepClone(), ["identity"] = row["identity"].DeepClone(),
                ["serializedFields"] = ((JObject)row["fields"]).Count, ["hierarchyObjects"] = row["objects"].Count() })),
            ["presentationVariantScaleRoundingDigits"] = 5,
            ["presentationVariants"] = new JArray(snapshot["actors"].GroupBy(row => (string)row["scene"]).Select(scene => new JObject
            {
                ["scene"] = scene.Key,
                ["variants"] = new JArray(scene.GroupBy(row => row["presentationVariant"].ToString(Newtonsoft.Json.Formatting.None)).Select(group => new JObject
                {
                    ["configuration"] = JObject.Parse(group.Key), ["count"] = group.Count(),
                    ["representativeHierarchy"] = group.First()["hierarchy"].DeepClone(),
                    ["actorHierarchies"] = new JArray(group.Select(row => row["hierarchy"].DeepClone()))
                }))
            })),
            ["sceneSaves"] = 0, ["priorLoadedAndDirtyStatePreserved"] = true, ["runtimeHasIk"] = runtimeHasIk,
            ["playModeStartScene"] = EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : null,
            ["authoringGate"] = "Not applicable: no scene objects added, moved or reparented by this read-only audit" };
    }

    private static object Compact(JObject report) => new
    {
        actors = (int)report["count"],
        scenes = report["scenes"],
        status = (string)report["status"],
        priorLoadedAndDirtyStatePreserved = (bool)report["priorLoadedAndDirtyStatePreserved"],
        sceneSaves = 0,
        baseline = BaselinePath,
        report = ReportPath,
        baselineSha256 = (string)report["baselineSha256"],
        currentBodyAndAttackSetsVerified = (bool?)report["currentBodyAndAttackSetsVerified"] ?? false
    };

    private static JArray ScaleGroup(Vector3 scale) => new JArray(Math.Round(scale.x, 5), Math.Round(scale.y, 5), Math.Round(scale.z, 5));

    private static string HashFile(string path)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }

    private static string Hierarchy(Transform value) => value.parent == null ? value.name : Hierarchy(value.parent) + "/" + value.name;

    private static void RequireStopped()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before the authored Stalker preservation audit.");
        }
    }
}
