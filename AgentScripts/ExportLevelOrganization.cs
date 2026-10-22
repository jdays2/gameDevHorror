using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExportLevelOrganization
{
    public static object CompareSavedData(string sceneName)
    {
        string path = "Assets/EmergencyShift/Scenes/" + sceneName + ".unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        var memory = LevelAuthoringState.SavedFields(scene);
        Scene preview = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var saved = LevelAuthoringState.SavedFields(preview);
            var differences = memory.Where(pair => !saved.TryGetValue(pair.Key, out string value) || value != pair.Value)
                .Select(pair => new { field = pair.Key, memory = pair.Value, saved = saved.TryGetValue(pair.Key, out string value) ? value : "MISSING" }).ToArray();
            var result = new { scene = path, dirty = scene.isDirty, memoryFields = memory.Count, savedFields = saved.Count,
                differences, savedOnly = saved.Keys.Except(memory.Keys).ToArray() };
            File.WriteAllText("work/level-reuse-20261022/" + sceneName + "-memory-compare.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return new { scene = path, dirty = scene.isDirty, memoryFields = memory.Count, savedFields = saved.Count,
                differences = differences.Length, savedOnly = result.savedOnly.Length };
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    public static object SettleVerifiedSource(string sceneName)
    {
        CompareSavedData(sceneName);
        string evidence = "work/level-reuse-20261022/" + sceneName + "-memory-compare.json";
        var proof = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(evidence));
        if (((Newtonsoft.Json.Linq.JArray)proof["differences"]).Count != 0
            || ((Newtonsoft.Json.Linq.JArray)proof["savedOnly"]).Count != 0)
        {
            throw new InvalidOperationException("Unexplained authoring changes remain; do not save.");
        }
        Scene scene = SceneManager.GetSceneByPath((string)proof["scene"]);
        bool saved = EditorSceneManager.SaveScene(scene);
        return new { scene = scene.path, saved, scene.isDirty };
    }

    public static object PublishSceneCopy(string sceneName)
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("EditMode export required.");
        }
        const string sourceRoot = "Assets/EmergencyShift/Scenes/";
        const string recovery = "Assets/_Recovery/LevelReuse/";
        const string output = "work/level-reuse-20261022/publication/";
        Directory.CreateDirectory(recovery);
        Directory.CreateDirectory(output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string originalPath = sourceRoot + sceneName + ".unity";
        string copyPath = recovery + sceneName + "-publication.unity";
        Scene scene = SceneManager.GetSceneByPath(originalPath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Additive);
        }
        if (scene.isDirty)
        {
            throw new InvalidOperationException("Do not export over unsaved user changes: " + originalPath);
        }
        Scene copy = default;
        try
        {
            if (!EditorSceneManager.SaveScene(scene, copyPath, true))
            {
                throw new IOException("Unable to save a separate publication copy.");
            }
            AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceSynchronousImport);
            copy = EditorSceneManager.OpenScene(copyPath, OpenSceneMode.Additive);
            var excluded = copy.GetRootGameObjects().Where(root => root.name == "Pause Menu").ToArray();
            foreach (var root in excluded)
            {
                // Only the isolated publication copy loses the original unfinished draft feature.
                UnityEngine.Object.DestroyImmediate(root);
            }
            object references = OrganizeLevelScenes.VerifyReferences(copy);
            EditorSceneManager.MarkSceneDirty(copy);
            if (!EditorSceneManager.SaveScene(copy))
            {
                throw new IOException("Unable to save sanitized publication copy.");
            }
            string destination = output + sceneName + ".unity";
            File.Copy(copyPath, destination, true);
            var result = new { original = originalPath, publication = destination, removedDraftRoots = excluded.Length,
                sourceStillClean = !scene.isDirty, references };
            File.WriteAllText(output + sceneName + ".json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            if (copy.IsValid() && copy.isLoaded && !copy.isDirty)
            {
                EditorSceneManager.CloseScene(copy, true);
            }
            if (opened && !scene.isDirty)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
