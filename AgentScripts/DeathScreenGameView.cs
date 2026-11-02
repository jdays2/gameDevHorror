using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

public static class DeathScreenGameView
{
    public static object Inspect()
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type view = assembly.GetType("UnityEditor.GameView");
        Type sizes = assembly.GetType("UnityEditor.GameViewSizes");
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        object instance = sizes.GetProperty("instance", flags).GetValue(null);
        object group = sizes.GetProperty("currentGroup", flags).GetValue(instance);
        return new
        {
            gameView = view?.FullName,
            viewProperties = view?.GetProperties(flags).Where(item => item.Name.IndexOf("size", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => new { item.Name, type = item.PropertyType.FullName, item.CanWrite }).ToArray(),
            viewMethods = view?.GetMethods(flags).Where(item => item.Name.IndexOf("SizeSelection", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.ToString()).ToArray(),
            sizes = sizes?.FullName,
            sizeProperties = sizes?.GetProperties(flags).Select(item => new { item.Name, type = item.PropertyType.FullName }).ToArray(),
            sizeMethods = sizes?.GetMethods(flags).Where(item => item.Name.Contains("Group")).Select(item => item.ToString()).ToArray(),
            entryConstructors = assembly.GetType("UnityEditor.GameViewSize")?.GetConstructors(flags).Select(item => item.ToString()).ToArray(),
            entryProperties = assembly.GetType("UnityEditor.GameViewSize").GetProperties(flags).Select(item => item.Name).ToArray(),
            groupMethods = group.GetType().GetMethods(flags).Where(item => item.Name.Contains("Size") || item.Name.Contains("Count"))
                .Select(item => item.ToString()).ToArray(),
            sizeKinds = Enum.GetNames(assembly.GetType("UnityEditor.GameViewSizeType"))
        };
    }
}
