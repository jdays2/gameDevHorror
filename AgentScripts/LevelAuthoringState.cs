using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Semantic before/after guard: parent/local transform and prefab bookkeeping may change,
// while existing components, referenced objects and world-space presentation must survive.
public sealed class LevelAuthoringState
{
    private readonly Dictionary<Transform, Matrix4x4> poses = new Dictionary<Transform, Matrix4x4>();
    private readonly Dictionary<GameObject, bool> active = new Dictionary<GameObject, bool>();
    private readonly Dictionary<Component, Dictionary<string, string>> components = new Dictionary<Component, Dictionary<string, string>>();

    public LevelAuthoringState(Scene scene)
    {
        foreach (var transform in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
        {
            // Only empty hierarchy wrappers are allowed to be removed.
            if (transform.GetComponents<Component>().Length == 1 && !PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
            {
                continue;
            }
            poses[transform] = transform.localToWorldMatrix;
            active[transform.gameObject] = transform.gameObject.activeInHierarchy;
            foreach (var component in transform.GetComponents<Component>().Where(component => component != null && !(component is Transform)))
            {
                components[component] = Fields(component);
            }
        }
    }

    public object AssertPreserved()
    {
        foreach (var pair in poses)
        {
            if (pair.Key == null)
            {
                throw new InvalidOperationException("A non-wrapper Transform was removed.");
            }
            var current = pair.Key.localToWorldMatrix;
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(pair.Value[index] - current[index]) > 0.00005f)
                {
                    throw new InvalidOperationException("World transform changed: " + pair.Key.name);
                }
            }
            if (pair.Key.gameObject.activeInHierarchy != active[pair.Key.gameObject])
            {
                throw new InvalidOperationException("Active state changed: " + pair.Key.name);
            }
        }
        foreach (var pair in components)
        {
            if (pair.Key == null)
            {
                throw new InvalidOperationException("An authored component was replaced or removed.");
            }
            var current = Fields(pair.Key);
            foreach (var field in pair.Value)
            {
                if (!current.TryGetValue(field.Key, out string value) || value != field.Value)
                {
                    throw new InvalidOperationException("Authored field changed: " + pair.Key.name + "." + field.Key
                        + " (" + field.Value + " -> " + value + ")");
                }
            }
        }
        return new { worldTransforms = poses.Count, authoredComponents = components.Count, unchanged = true };
    }

    private static Dictionary<string, string> Fields(Component component)
    {
        var result = new Dictionary<string, string>();
        using (var serialized = new SerializedObject(component))
        {
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyPath.StartsWith("m_Prefab") || property.propertyPath.StartsWith("m_Corresponding")
                    || property.propertyType == SerializedPropertyType.Generic)
                {
                    continue;
                }
                string value = Value(property);
                if (value != null)
                {
                    result[property.propertyPath] = value;
                }
            }
        }
        return result;
    }

    public static Dictionary<string, string> SavedFields(Scene scene)
    {
        var result = new Dictionary<string, string>();
        foreach (var gameObject in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(transform => transform.gameObject))
        {
            var owners = new UnityEngine.Object[] { gameObject }.Concat(gameObject.GetComponents<Component>());
            foreach (var owner in owners)
            {
                string id = GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString();
                using (var serialized = new SerializedObject(owner))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.Generic || property.propertyPath.StartsWith("m_Prefab")
                            || property.propertyPath.StartsWith("m_Corresponding")
                            || property.propertyPath.EndsWith(".m_FileID") || property.propertyPath.EndsWith(".m_PathID"))
                        {
                            continue;
                        }
                        if (owner is RectTransform rect && (property.propertyPath.StartsWith("m_LocalPosition")
                            || rect.TryGetComponent<Canvas>(out var canvas) && canvas != null && canvas.renderMode != RenderMode.WorldSpace
                            && (property.propertyPath.StartsWith("m_Local") || property.propertyPath.StartsWith("m_Anchored")
                                || property.propertyPath.StartsWith("m_SizeDelta") || property.propertyPath.StartsWith("m_Pivot")
                                || property.propertyPath.StartsWith("m_Anchor"))))
                        {
                            continue; // Rect local position derives from anchors; screen Canvas geometry derives from its viewport.
                        }
                        if (owner is Component lampView && lampView.GetComponentInParent<EmergencyShift.LocalLamp2D>(true) != null
                            && ((owner is UnityEngine.Rendering.Universal.Light2D
                                && (property.propertyPath == "m_Enabled" || property.propertyPath == "m_Intensity"))
                                || owner is SpriteRenderer && property.propertyPath == "m_Enabled"))
                        {
                            continue; // LocalLamp settings/profile are authoritative; preview OnDisable changes the native view.
                        }
                        string value;
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            value = property.objectReferenceValue == null ? "null"
                                : GlobalObjectId.GetGlobalObjectIdSlow(property.objectReferenceValue).ToString();
                        }
                        else
                        {
                            value = Value(property);
                        }
                        if (value != null)
                        {
                            result[id + "|" + property.propertyPath] = value;
                        }
                    }
                }
            }
        }
        return result;
    }

    private static string Value(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == null ? "null:" + property.objectReferenceEntityIdValue
                    : property.objectReferenceValue.GetEntityId().ToString();
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
                return null; // vectors/colours/rects/bounds are traversed into their scalar children.
        }
    }
}
