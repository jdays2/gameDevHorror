using System;
using System.IO;
using System.Linq;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reimport existing textures/audio and inspect their actual users. No scene writes.</summary>
public static class RefreshScorpionArt
{
    public static object Import()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped compiled Editor.");
        }
        string before = SceneState();
        string[] paths = {
            "Assets/EmergencyShift/Art/Enemies/Scorpion/Neutral.png",
            "Assets/EmergencyShift/Art/Enemies/Scorpion/Run.png",
            "Assets/EmergencyShift/Art/Enemies/Scorpion/Attack.png",
            "Assets/EmergencyShift/Audio/Enemies/Scorpion/Attack.wav"
        };
        string[] guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
        foreach (string path in paths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        var set = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>("Assets/EmergencyShift/Data/Enemies/Scorpion/Presentation.asset");
        set.Validate();
        var scene = SceneManager.GetActiveScene();
        var actors = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Scorpion2D>(true)).ToArray();
        bool scenesUnchanged = before == SceneState();
        var report = new
        {
            revision = "scorpion-native-r8-heavy-chela",
            guidsPreserved = guids.SequenceEqual(paths.Select(AssetDatabase.AssetPathToGUID)),
            textures = paths.Take(3).Select(path => new
            {
                path,
                guid = AssetDatabase.AssetPathToGUID(path),
                sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count(),
                width = AssetDatabase.LoadAssetAtPath<Texture2D>(path).width,
                height = AssetDatabase.LoadAssetAtPath<Texture2D>(path).height
            }).ToArray(),
            attackSeconds = set.Attack.Duration,
            runSeconds = set.Run.Duration,
            audioSeconds = set.Grasp.length,
            neutralUsesRunStart = set.Idle == set.Run.FrameAt(0),
            runUsesTexture = AssetDatabase.GetAssetPath(set.Run.FrameAt(0).texture),
            attackUsesTexture = AssetDatabase.GetAssetPath(set.Attack.FrameAt(.224f).texture),
            actors = actors.Select(actor => new
            {
                actor.name,
                presentationShared = actor.Presentation == set,
                audioSources = actor.GetComponentsInChildren<AudioSource>(true).Length,
                missingScripts = actor.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))
            }).ToArray(),
            scene = scene.path,
            scenesUnchanged,
            sceneSavePerformed = false,
            authoringGate = "Not applicable: existing texture/audio resources only; no scene object edits."
        };
        if (!scenesUnchanged)
        {
            throw new InvalidOperationException("Loaded scene state changed during reimport.");
        }
        File.WriteAllText("Docs/Validation/CreatureVisuals01/scorpion-r8.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static string SceneState()
    {
        return string.Join("|", Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
        {
            var scene = SceneManager.GetSceneAt(index);
            return scene.handle + ":" + scene.path + ":" + scene.isDirty + ":" + scene.rootCount;
        }));
    }
}
