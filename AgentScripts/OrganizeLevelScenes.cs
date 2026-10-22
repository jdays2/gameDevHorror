using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrganizeLevelScenes
{
    private const string Evidence = "work/level-reuse-20261022/";

    public static object Apply(string sceneName)
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before scene organization.");
        }
        var plans = JArray.Parse(File.ReadAllText("Concepts/Used/Environment/LevelOrganization01/migration-map.json"));
        var plan = plans.Single(row => Path.GetFileNameWithoutExtension((string)row["scene"]) == sceneName);
        string path = (string)plan["scene"];
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        if (scene.isDirty)
        {
            throw new InvalidOperationException("Preserve unrelated unsaved changes before authoring " + path);
        }
        var before = new LevelAuthoringState(scene);
        var original = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var moves = ((JArray)plan["moves"]).Select(row => new
        {
            transform = Resolve((string)row["id"], scene),
            parent = (string)row["parent"]
        }).ToArray();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Organize level zones and reusable surfaces");
        foreach (var root in scene.GetRootGameObjects())
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Preserve authored level");
        }
        try
        {
            int assemblies = LevelReuseLibrary.ConnectAssemblies(scene);
            int connected = LevelReuseLibrary.ConnectLooseSurfaces(scene);
            var groups = new Dictionary<string, Transform>();
            foreach (var row in plan["groups"])
            {
                string groupPath = (string)row["path"];
                var group = new GameObject(groupPath.Split('/').Last());
                SceneManager.MoveGameObjectToScene(group, scene);
                Undo.RegisterCreatedObjectUndo(group, "Create level group");
                int slash = groupPath.LastIndexOf('/');
                if (slash >= 0)
                {
                    group.transform.SetParent(groups[groupPath.Substring(0, slash)], false);
                }
                groups.Add(groupPath, group.transform);
            }
            foreach (var move in moves)
            {
                Undo.SetTransformParent(move.transform, groups[move.parent], true, "Group authored object");
                if (PrefabUtility.IsPartOfPrefabInstance(move.transform))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(move.transform);
                }
            }
            var orderedRoots = scene.GetRootGameObjects().OrderBy(root => root.name == "Pause Menu" ? "99_Pause" : root.name).ToArray();
            for (int index = 0; index < orderedRoots.Length; index++)
            {
                orderedRoots[index].transform.SetSiblingIndex(index);
            }
            var protectedRoots = ((JArray)plan["preserveRoots"]).Select(row => (string)row).ToArray();
            int removed = 0;
            foreach (var transform in original.Reverse())
            {
                if (transform != null && transform.childCount == 0 && transform.GetComponents<Component>().Length == 1
                    && !LevelReuseLibrary.HasPrefabAncestor(transform) && !protectedRoots.Contains(transform.name))
                {
                    Undo.DestroyObjectImmediate(transform.gameObject);
                    removed++;
                }
            }
            object preservation = before.AssertPreserved();
            var references = VerifyReferences(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new IOException("Scene save failed: " + path);
            }
            var result = new { scene = path, connectedSurfaces = connected, assemblies, groups = groups.Count,
                movedObjects = moves.Length, removedEmptyWrappers = removed, preservation, references,
                roots = scene.GetRootGameObjects().Select(root => root.name).ToArray() };
            File.WriteAllText(Evidence + sceneName + "-result.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            Undo.CollapseUndoOperations(undoGroup);
            return result;
        }
        finally
        {
            if (opened && !scene.isDirty)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
        }
    }

    private static Transform Resolve(string id, Scene scene)
    {
        if (!GlobalObjectId.TryParse(id, out var parsed))
        {
            throw new InvalidOperationException("Invalid authoring identity: " + id);
        }
        var gameObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsed) as GameObject;
        if (gameObject == null || gameObject.scene != scene)
        {
            throw new InvalidOperationException("Authored object not found in " + scene.path + ": " + id);
        }
        return gameObject.transform;
    }

    public static object VerifyReferences(Scene scene)
    {
        int components = 0;
        int references = 0;
        foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)))
        {
            if (component == null)
            {
                throw new InvalidOperationException(scene.path + ": missing script");
            }
            components++;
            using (var serialized = new SerializedObject(component))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        references++;
                        if (property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default)
                        {
                            throw new InvalidOperationException(component.name + ": unresolved " + property.propertyPath);
                        }
                    }
                }
            }
        }
        return new { components, references, missingScripts = 0, unresolvedReferences = 0 };
    }
}
