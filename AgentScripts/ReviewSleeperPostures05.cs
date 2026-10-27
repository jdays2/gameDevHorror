using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Read-only preservation audit of the three authored Sleeper instances.</summary>
public static class ReviewSleeperPostures05
{
    private const string Folder = "Concepts/Used/Sleeper/Postures05/";
    private const string FullBaseline = Folder + "scene-components-before.json";

    public static object CaptureBaseline()
    {
        RequireStopped();
        if (File.Exists(FullBaseline))
        {
            return new { preservedExistingBaseline = true, path = FullBaseline };
        }

        var actors = new JArray();
        ReadScenes((scene, actor, saved) =>
        {
            var row = new JObject
            {
                ["scene"] = scene.path,
                ["id"] = actor.GetComponent<PersistentEntity2D>().Id,
                ["actorGlobalId"] = GlobalObjectId.GetGlobalObjectIdSlow(actor).ToString(),
                ["animatorGlobalId"] = GlobalObjectId.GetGlobalObjectIdSlow(actor.GetComponent<CreatureSpriteAnimator2D>()).ToString(),
                ["sourceInstanceFileId"] = (string)saved["prefabInstance"],
                ["fields"] = SnapshotFields(actor),
                ["position"] = actor.transform.position.ToString("R"),
                ["rotation"] = actor.transform.rotation.ToString("R"),
                ["scale"] = actor.transform.lossyScale.ToString("R")
            };
            actors.Add(row);
        });
        File.WriteAllText(FullBaseline, new JObject { ["actors"] = actors, ["scenesSaved"] = 0 }.ToString());
        return new { actors = actors.Count, scenesSaved = 0, path = FullBaseline };
    }

    public static object Verify()
    {
        RequireStopped();
        if (!File.Exists(FullBaseline))
        {
            throw new InvalidOperationException("CaptureBaseline must run before updating Sleeper presentation to preserve all authored component fields.");
        }

        JObject baseline = JObject.Parse(File.ReadAllText(FullBaseline));
        var rows = new JArray();
        ReadScenes((scene, actor, saved) =>
        {
            string id = actor.GetComponent<PersistentEntity2D>().Id;
            JToken before = baseline["actors"].Single(row => (string)row["scene"] == scene.path && (string)row["id"] == id);
            if ((string)before["actorGlobalId"] != GlobalObjectId.GetGlobalObjectIdSlow(actor).ToString() ||
                (string)before["position"] != actor.transform.position.ToString("R") ||
                (string)before["rotation"] != actor.transform.rotation.ToString("R") ||
                (string)before["scale"] != actor.transform.lossyScale.ToString("R"))
            {
                throw new InvalidOperationException("Sleeper identity or world pose changed: " + actor.name);
            }

            JObject priorFields = (JObject)before["fields"];
            JObject currentFields = SnapshotFields(actor);
            string bodySpriteField = GlobalObjectId.GetGlobalObjectIdSlow(actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>()) + "|m_Sprite";
            int legacyReferenceInternals = 0;
            foreach (JProperty field in priorFields.Properties())
            {
                if (LegacyReferenceInternal(field.Name, priorFields, currentFields, bodySpriteField))
                {
                    legacyReferenceInternals++;
                    continue;
                }
                if (!JToken.DeepEquals(field.Value, currentFields[field.Name]))
                {
                    throw new InvalidOperationException("Authored Sleeper field changed: " + actor.name + " | " + field.Name);
                }
            }

            var animator = actor.GetComponent<SleeperSpriteAnimator>();
            if (animator == null || actor.GetComponents<CreatureSpriteAnimator2D>().Length != 1 ||
                (string)before["animatorGlobalId"] != GlobalObjectId.GetGlobalObjectIdSlow(animator).ToString() ||
                AssetDatabase.GetAssetPath(animator.SpriteSet) != "Assets/EmergencyShift/Data/Enemies/SleeperSprites05.asset")
            {
                throw new InvalidOperationException("Sleeper body matrix is not inherited by the actual scene actor: " + actor.name);
            }

            animator.SpriteSet.Validate();
            var animatorFields = new SerializedObject(animator);
            var attack = animatorFields.FindProperty("attackSpriteSet").objectReferenceValue as CreatureAttackSpriteSet;
            if (attack == null || AssetDatabase.GetAssetPath(attack) != "Assets/EmergencyShift/Data/Enemies/SleeperAttackSprites05.asset")
            {
                throw new InvalidOperationException("Sleeper attack palette is missing from actual scene actor: " + actor.name);
            }

            attack.Validate();
            foreach (Transform child in actor.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                {
                    throw new InvalidOperationException("Missing script in Sleeper hierarchy: " + child.name);
                }
            }

            if (actor.GetComponentsInChildren<MeshRenderer>(true).Length != 0)
            {
                throw new InvalidOperationException("Unexpected runtime limb mesh in Sleeper hierarchy: " + actor.name);
            }

            VerifySavedOverrides(saved);
            var body = actor.GetComponent<Rigidbody2D>();
            var crowd = actor.GetComponent<CrowdAgent2D>();
            rows.Add(new JObject
            {
                ["scene"] = scene.path,
                ["name"] = actor.name,
                ["id"] = id,
                ["position"] = actor.transform.position.ToString("R"),
                ["rotation"] = actor.transform.rotation.ToString("R"),
                ["scale"] = actor.transform.lossyScale.ToString("R"),
                ["actorGlobalId"] = GlobalObjectId.GetGlobalObjectIdSlow(actor).ToString(),
                ["animatorGlobalId"] = GlobalObjectId.GetGlobalObjectIdSlow(animator).ToString(),
                ["sourceInstanceFileId"] = (string)saved["prefabInstance"],
                ["attacksEnabled"] = actor.AttacksEnabled,
                ["constraints"] = body.constraints.ToString(),
                ["crowdEnabled"] = crowd.enabled,
                ["allPriorAuthoredComponentFieldsPreserved"] = true,
                ["legacyNativeReferenceInternalsExcluded"] = legacyReferenceInternals,
                ["savedOverridesPreserved"] = true,
                ["missingScripts"] = 0,
                ["runtimeMeshes"] = 0,
                ["sceneDirty"] = scene.isDirty
            });
        });

        bool runtimeHasIk = typeof(SleeperSpriteAnimator).Assembly.GetType("EmergencyShift.Features.Enemies.Logic.StalkerGaitModel") != null;
        if (runtimeHasIk)
        {
            throw new InvalidOperationException("Authoring IK is compiled into the runtime assembly.");
        }

        var report = new JObject
        {
            ["actors"] = rows,
            ["count"] = rows.Count,
            ["runtimeHasIk"] = runtimeHasIk,
            ["scenesSaved"] = 0,
            ["playModeStartScene"] = EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : null,
            ["authoringGate"] = "No scene objects added, moved or reparented by this read-only audit"
        };
        File.WriteAllText(Folder + "preservation.json", report.ToString());
        return report;
    }

    private static void ReadScenes(Action<Scene, Sleeper2D, JToken> inspect)
    {
        JArray baseline = JArray.Parse(File.ReadAllText(Folder + "scene-instances-before.json"));
        Scene active = SceneManager.GetActiveScene();
        foreach (IGrouping<string, JToken> group in baseline.GroupBy(row => (string)row["scene"]))
        {
            Scene scene = SceneManager.GetSceneByPath(group.Key);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(group.Key, OpenSceneMode.Additive);
            }

            bool dirtyBefore = scene.isDirty;
            try
            {
                Sleeper2D[] actors = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Sleeper2D>(true)).ToArray();
                if (actors.Length != group.Count())
                {
                    throw new InvalidOperationException("Authored Sleeper count changed: " + scene.path);
                }

                foreach (JToken saved in group)
                {
                    string text = (string)saved["overrides"];
                    string id = Regex.Match(text, @"propertyPath: persistentId\s+value: ([^\r\n]+)").Groups[1].Value;
                    Sleeper2D actor = actors.Single(value => value.GetComponent<PersistentEntity2D>().Id == id);
                    inspect(scene, actor, saved);
                }

                if (dirtyBefore != scene.isDirty)
                {
                    throw new InvalidOperationException("Read-only Sleeper inspection dirtied a scene: " + scene.path);
                }
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (active.IsValid() && active.isLoaded)
                {
                    SceneManager.SetActiveScene(active);
                }
            }
        }
    }

    private static JObject SnapshotFields(Sleeper2D actor)
    {
        var result = new JObject();
        SpriteRenderer body = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        foreach (Component component in actor.GetComponentsInChildren<Component>(true))
        {
            if (component == null || component is CreatureSpriteAnimator2D)
            {
                continue;
            }

            string owner = GlobalObjectId.GetGlobalObjectIdSlow(component).ToString();
            using (var serialized = new SerializedObject(component))
            {
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    // GlobalObjectId preserves the real GUID/fileID; PPtr children are
                    // temporary editor handles and must not enter a preservation baseline.
                    enterChildren = property.propertyType != SerializedPropertyType.ObjectReference;
                    string path = property.propertyPath;
                    if (path.StartsWith("m_Prefab") || path.StartsWith("m_Corresponding") ||
                        path == "m_EditorClassIdentifier" || component == body && path == "m_Sprite")
                    {
                        continue;
                    }

                    string value = FieldValue(property);
                    if (value != null)
                    {
                        result[owner + "|" + path] = value;
                    }
                }
            }
        }

        return result;
    }

    private static bool LegacyReferenceInternal(string key, JObject before, JObject current, string bodySpriteField)
    {
        if (!key.EndsWith(".m_FileID", StringComparison.Ordinal) && !key.EndsWith(".m_PathID", StringComparison.Ordinal))
        {
            return false;
        }

        string parent = key.Substring(0, key.LastIndexOf('.'));
        if (parent == bodySpriteField)
        {
            // The body sprite is the explicit presentation change, already checked above.
            return true;
        }

        string reference = before[parent]?.Value<string>();
        bool stableReference = reference == "null" || reference != null && reference.StartsWith("GlobalObjectId_V1-", StringComparison.Ordinal);
        // Accept a legacy native child only when its complete persistent parent reference
        // is still present and exactly equal. Authored IDs and fields remain strict.
        return stableReference && current[parent] != null && JToken.DeepEquals(before[parent], current[parent]);
    }

    private static string FieldValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                return ReferenceValue(property.objectReferenceValue);
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

    private static string ReferenceValue(UnityEngine.Object value)
    {
        if (value == null)
        {
            return "null";
        }

        string identity = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
        string path = AssetDatabase.GetAssetPath(value);
        if (!(value is ScriptableObject) || !File.Exists(path))
        {
            return identity;
        }

        using (SHA256 hash = SHA256.Create())
        {
            string digest = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
            return identity + "|sha256:" + digest;
        }
    }

    private static void VerifySavedOverrides(JToken saved)
    {
        string path = (string)saved["scene"];
        string id = (string)saved["prefabInstance"];
        string current = Regex.Match(File.ReadAllText(path), @"(?ms)^--- !u!1001 &" + id + @"\s.*?(?=^--- !u!|\z)").Value.Replace("\r\n", "\n");
        string before = ((string)saved["overrides"]).Replace("\r\n", "\n");
        const string pattern = @"(?ms)- target: \{[^\r\n]+\}\s+propertyPath: [^\r\n]+\s+value: [^\r\n]*\s+objectReference: \{[^\r\n]+\}";
        foreach (Match field in Regex.Matches(before, pattern))
        {
            if (!current.Contains(field.Value))
            {
                throw new InvalidOperationException("Saved Sleeper override changed: " + path + " | " + field.Value);
            }
        }
    }

    private static void RequireStopped()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before inspecting authored Sleeper instances.");
        }
    }
}
