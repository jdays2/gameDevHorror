using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Apply the owner's selected r12 palette to live bodies and their remains.</summary>
public static class ApplyScorpionPaletteR13
{
    private const string Root = "Assets/EmergencyShift/";
    private const string Folder = "Docs/Validation/ScorpionPaletteR13/";

    public static object Apply()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped, compiled Editor.");
        }
        string scenesBefore = SceneState();
        Material selected = SelectedMaterial();
        foreach (string name in new[] { "Scorpion", "ScorpionVisual" })
        {
            string path = Root + "Prefabs/Enemies/" + name + ".prefab";
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var body = contents.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.shader == selected.shader);
                body.sharedMaterial = selected;
                var gore = contents.GetComponent<GoreBurst2D>();
                if (gore != null)
                {
                    var serializedGore = new SerializedObject(gore);
                    serializedGore.FindProperty("litMaterial").objectReferenceValue = selected;
                    serializedGore.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
        if (scenesBefore != SceneState())
        {
            throw new InvalidOperationException("Authored scene state changed.");
        }
        return Verify();
    }

    public static object Verify()
    {
        Material selected = SelectedMaterial();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/Scorpion.prefab");
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/ScorpionVisual.prefab");
        var body = prefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        var gore = new SerializedObject(prefab.GetComponent<GoreBurst2D>());
        bool bodyAndRemainsMatch = body.sharedMaterial == selected
            && gore.FindProperty("litMaterial").objectReferenceValue == selected
            && visual.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sharedMaterial == selected);
        var actors = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt)
            .SelectMany(scene => scene.GetRootGameObjects())
            .SelectMany(root => root.GetComponentsInChildren<Scorpion2D>(true))
            .Select(actor => new
            {
                actor.name,
                position = actor.transform.position.ToString(),
                body = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sharedMaterial.name,
                remains = ((Material)new SerializedObject(actor.GetComponent<GoreBurst2D>())
                    .FindProperty("litMaterial").objectReferenceValue).name
            }).ToArray();
        if (!bodyAndRemainsMatch || actors.Any(actor => actor.body != selected.name || actor.remains != selected.name))
        {
            throw new InvalidOperationException("Selected palette did not reach every body and remains source.");
        }
        int shaderErrors = ShaderUtil.GetShaderMessages(selected.shader).Count(message => message.severity.ToString() == "Error");
        if (shaderErrors != 0)
        {
            throw new InvalidOperationException("Selected palette shader has errors.");
        }
        var report = new
        {
            task = "TASK-005",
            revision = "blush-selected-r13",
            material = AssetDatabase.GetAssetPath(selected),
            guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selected)),
            bodyAndRemainsMatch,
            shaderErrors,
            playing = Application.isPlaying,
            actors,
            scenes = SceneState(),
            sceneSaved = false
        };
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + (Application.isPlaying ? "play.json" : "apply.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static Material SelectedMaterial()
    {
        var spec = JObject.Parse(File.ReadAllText("Concepts/Unused/Visual/Scorpion01/palette-variants-r12.json"));
        var palette = spec["palettes"].Single(item => (string)item["key"] == (string)spec["default"]);
        var selected = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Characters/Scorpion/" + (string)palette["material"] + ".mat");
        return selected != null ? selected : throw new InvalidOperationException("Selected material is missing.");
    }

    private static string SceneState() => string.Join("|", Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
    {
        var scene = SceneManager.GetSceneAt(index);
        return scene.handle + ":" + scene.path + ":" + scene.isDirty + ":" + scene.rootCount;
    }));
}
