using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

// Opt-in read-only native dependency check before deleting the internal pass-through type.
// Complements, rather than replaces, full-text/dynamic-use and actual build checks.
public static class Task008NavigationReferences
{
    public static object Inspect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            throw new InvalidOperationException("Requires idle stopped Editor and the game lease.");
        }
        const string source = "Assets/EmergencyShift/Runtime/Enemies/Common/EnemyChasePath2D.cs";
        const string expectedGuid = "3173718c069f4314a8a68a07766839d7";
        string output = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
            "../Docs/Validation/CodeReadabilityTask008/navigation.native-references.before.json"));
        if (File.Exists(output))
        {
            throw new IOException("Preserve existing native dependency evidence.");
        }
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(source);
        Type type = typeof(EmergencyShift.EnemyMotor2D).Assembly.GetType("EmergencyShift.EnemyChasePath2D", true);
        if (script == null || type.FullName != "EmergencyShift.EnemyChasePath2D" ||
            AssetDatabase.AssetPathToGUID(source) != expectedGuid)
        {
            throw new InvalidOperationException("The intended existing wrapper/type/GUID changed.");
        }
        var users = new JArray();
        string[] paths = AssetDatabase.GetAllAssetPaths();
        int inspected = 0;
        foreach (string path in paths)
        {
            if ((!path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith("Packages/", StringComparison.Ordinal)) ||
                path == source || AssetDatabase.IsValidFolder(path))
            {
                continue;
            }
            inspected++;
            if (AssetDatabase.GetDependencies(path, false).Contains(source))
            {
                users.Add(path);
            }
        }
        var report = new JObject
        {
            ["source"] = source,
            ["guid"] = expectedGuid,
            ["type"] = type.FullName,
            ["internal"] = type.IsNotPublic,
            ["unityObject"] = typeof(UnityEngine.Object).IsAssignableFrom(type),
            ["serializableAttribute"] = type.IsDefined(typeof(SerializableAttribute), false),
            ["assetsInspected"] = inspected,
            ["directNativeUsers"] = users,
            ["limits"] = "Native dependency database only. Source/dynamic callers, serialized names/GUID text, actual model behavior and ordinary build must also be checked; no deletion performed."
        };
        File.WriteAllText(output, report.ToString());
        return report;
    }
}
