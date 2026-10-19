using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

public static class AuditProjectReferences
{
    private const string Root = "Assets/EmergencyShift";

    public static object Read()
    {
        string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { Root })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
        var sceneDependencies = new Dictionary<string, HashSet<string>>();
        foreach (string scene in scenes)
        {
            sceneDependencies.Add(scene, new HashSet<string>(AssetDatabase.GetDependencies(scene, true)));
        }

        string[] serializedPaths = Directory.GetFiles(Root, "*", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => new[] { ".unity", ".prefab", ".asset", ".mat", ".controller", ".overrideController" }.Contains(Path.GetExtension(path)))
            .OrderBy(path => path).ToArray();
        var references = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<object>();
        var expression = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);
        foreach (string path in serializedPaths)
        {
            string text = File.ReadAllText(path);
            foreach (Match match in expression.Matches(text))
            {
                string guid = match.Groups[1].Value;
                if (!references.TryGetValue(guid, out HashSet<string> users))
                {
                    users = new HashSet<string>();
                    references.Add(guid, users);
                }

                users.Add(path);
                if (!guid.StartsWith("0000000000000000", StringComparison.Ordinal) && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    missing.Add(new { asset = path, guid });
                }
            }
        }

        object[] scripts = Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/')).OrderBy(path => path)
            .Select(path =>
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                return (object)new
                {
                    path,
                    guid,
                    serializedUsers = references.TryGetValue(guid, out HashSet<string> users) ? users.OrderBy(user => user).ToArray() : Array.Empty<string>(),
                    dependencyScenes = sceneDependencies.Where(pair => pair.Value.Contains(path)).Select(pair => pair.Key).ToArray()
                };
            }).ToArray();

        return new
        {
            method = "AssetDatabase dependency graph and serialized external GUID resolution; no scene save, deletion or unused-code claim.",
            scripts,
            scenes,
            serializedAssetCount = serializedPaths.Length,
            unresolvedExternalReferences = missing,
            limits = "Does not prove local fileID validity, required-null policy, reflection-only use or art/behavior correctness."
        };
    }
}
