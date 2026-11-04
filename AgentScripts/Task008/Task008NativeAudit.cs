using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit TASK-008 evidence helper. Outside Assets; never saves or loads scenes.
public static class Task008NativeAudit
{
    private const string Evidence = "Docs/Validation/CodeReadabilityTask008";

    public static object Capture(string name)
    {
        JObject state = ReadState();
        WriteNew(name, state);
        return Compact(state, name);
    }

    public static object InspectSnapshot(string name) => Compact(ReadSnapshot(name), name);

    public static object Verify(string baselineName, string outputName)
    {
        JObject before = ReadSnapshot(baselineName);
        JObject after = ReadState();
        var differences = new JArray();
        var tokenDifferences = new JArray();
        foreach (string key in new[] { "playing", "playModeStartScene", "scenes" })
        {
            if (!JToken.DeepEquals(before[key], after[key]))
            {
                differences.Add(key);
                DescribeDifference(before[key], after[key], key, tokenDifferences);
            }
        }
        after["baseline"] = baselineName;
        after["differences"] = differences;
        after["tokenDifferences"] = tokenDifferences;
        after["preserved"] = differences.Count == 0;
        WriteNew(outputName, after);
        if (differences.Count != 0)
        {
            throw new InvalidOperationException("Loaded authored state differs; inspect " + Output(outputName));
        }
        return Compact(after, outputName);
    }

    private static JObject ReadState()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            throw new InvalidOperationException("Requires stopped, idle Editor; no state is changed by this helper.");
        }
        var scenes = new JArray();
        int missingScripts = 0;
        int missingReferences = 0;
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            var objects = new JArray();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if ((root.hideFlags & HideFlags.DontSave) != 0)
                {
                    continue;
                }
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    objects.Add(Describe(node.gameObject, ref missingReferences));
                    foreach (Component component in node.GetComponents<Component>())
                    {
                        if (component == null)
                        {
                            missingScripts++;
                            continue;
                        }
                        objects.Add(Describe(component, ref missingReferences));
                    }
                }
            }
            scenes.Add(new JObject
            {
                ["path"] = scene.path,
                ["loaded"] = scene.isLoaded,
                ["active"] = scene == SceneManager.GetActiveScene(),
                ["dirty"] = scene.isDirty,
                ["savedFileSha256"] = File.Exists(Path.Combine(Project(), scene.path))
                    ? new JValue(Hash(File.ReadAllBytes(Path.Combine(Project(), scene.path)))) : JValue.CreateNull(),
                ["objects"] = objects
            });
        }
        return new JObject
        {
            ["project"] = Project(),
            ["playing"] = EditorApplication.isPlaying,
            ["playModeStartScene"] = EditorSceneManager.playModeStartScene == null ? JValue.CreateNull()
                : new JValue(AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)),
            ["missingScripts"] = missingScripts,
            ["missingReferences"] = missingReferences,
            ["scenes"] = scenes,
            ["sceneSaves"] = 0,
            ["scope"] = "Loaded authored hierarchies, including inactive objects, full Editor JSON with native reference normalization, saved scene SHA and dirty/setup state. DontSave roots are excluded; closed scenes and Player behavior need separate validation."
        };
    }

    private static JObject Describe(UnityEngine.Object target, ref int missingReferences)
    {
        var document = JObject.Parse(EditorJsonUtility.ToJson(target));
        JObject fields = document.Properties().Count() == 1 && document.Properties().First().Value is JObject nested
            ? nested : document;
        using (var serialized = new SerializedObject(target))
        {
            SerializedProperty property = serialized.GetIterator();
            bool descend = true;
            while (property.Next(descend))
            {
                descend = property.propertyType != SerializedPropertyType.ObjectReference;
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }
                UnityEngine.Object value = property.objectReferenceValue;
                if (value == null && property.objectReferenceEntityIdValue != default)
                {
                    missingReferences++;
                }
                string path = property.propertyPath.Replace(".Array.data[", "[");
                JToken reference = fields.SelectToken(path, false);
                if (reference != null)
                {
                    reference.Replace(value == null ? new JValue("null") : new JValue(GlobalObjectId.GetGlobalObjectIdSlow(value).ToString()));
                }
            }
        }
        return new JObject
        {
            ["id"] = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString(),
            ["type"] = target.GetType().FullName,
            ["name"] = target.name,
            ["serializedSha256"] = Hash(Encoding.UTF8.GetBytes(document.ToString(Formatting.None))),
            ["serialized"] = document
        };
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }

    private static void DescribeDifference(JToken before, JToken after, string path, JArray result)
    {
        if (JToken.DeepEquals(before, after) || result.Count >= 12)
        {
            return;
        }
        if (before is JObject left && after is JObject right)
        {
            foreach (string name in left.Properties().Select(p => p.Name).Union(right.Properties().Select(p => p.Name)))
            {
                DescribeDifference(left[name], right[name], path + "/" + name, result);
            }
            return;
        }
        if (before is JArray first && after is JArray second && first.Count == second.Count)
        {
            for (int index = 0; index < first.Count; index++)
            {
                DescribeDifference(first[index], second[index], path + "/" + index, result);
            }
            return;
        }
        result.Add(new JObject { ["path"] = path, ["beforeType"] = before?.Type.ToString(),
            ["afterType"] = after?.Type.ToString(), ["before"] = before?.DeepClone(), ["after"] = after?.DeepClone() });
    }

    private static string Output(string name)
    {
        if (Path.GetFileName(name) != name || !name.EndsWith(".json", StringComparison.Ordinal))
        {
            throw new ArgumentException("Use a JSON filename inside task evidence.", nameof(name));
        }
        return Path.GetFullPath(Path.Combine(Project(), Evidence, name));
    }

    private static string Project() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    private static JObject ReadSnapshot(string name)
    {
        string path = Output(name);
        if (File.Exists(path))
        {
            return JObject.Parse(File.ReadAllText(path));
        }
        using (FileStream stream = File.OpenRead(path + ".gz"))
        using (var compressed = new GZipStream(stream, CompressionMode.Decompress))
        using (var reader = new StreamReader(compressed))
        {
            return JObject.Parse(reader.ReadToEnd());
        }
    }

    private static void WriteNew(string name, JObject value)
    {
        if (File.Exists(Output(name) + ".gz"))
        {
            throw new IOException("Preserve the existing compressed snapshot: " + name);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Output(name)));
        using (var stream = new FileStream(Output(name), FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(value.ToString(Formatting.Indented));
        }
    }

    private static object Compact(JObject state, string name) => new
    {
        report = File.Exists(Output(name)) ? Output(name) : Output(name) + ".gz",
        scenes = state["scenes"].Count(),
        objects = state["scenes"].Sum(scene => scene["objects"].Count()),
        missingScripts = (int)state["missingScripts"],
        missingReferences = (int)state["missingReferences"],
        preserved = state["preserved"],
        sceneSaves = 0
    };
}
