using System;
using System.IO;
using System.Linq;
using EmergencyShift.Features.Scorpion.Config;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Use the owner's selected discovery clip for quiet, spaced idle/running cues.</summary>
public static class ReviseScorpionSqueakR11
{
    private const string Root = "Assets/EmergencyShift/";

    public static object Install()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped compiled Editor.");
        }
        string before = SceneState();
        var presentation = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(Root + "Data/Enemies/Scorpion/Presentation.asset");
        AudioClip clip = presentation.Alarm;
        if (AssetDatabase.GetAssetPath(clip) != Root + "Audio/Enemies/Scorpion/Detection.wav")
        {
            throw new InvalidOperationException("The selected discovery sound was not found.");
        }
        var data = new SerializedObject(presentation);
        var squeaks = data.FindProperty("squeaks");
        squeaks.arraySize = 1;
        squeaks.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        data.FindProperty("presence").objectReferenceValue = clip;
        data.FindProperty("squeakVolume").floatValue = .22f;
        data.FindProperty("idleSqueakInterval").vector2Value = new Vector2(5, 8);
        data.FindProperty("runSqueakInterval").vector2Value = new Vector2(3, 5);
        data.ApplyModifiedPropertiesWithoutUndo();
        presentation.Validate();
        EditorUtility.SetDirty(presentation);
        AssetDatabase.SaveAssetIfDirty(presentation);
        string prefabPath = Root + "Prefabs/Enemies/Scorpion.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var source = prefab.transform.Find("Audio/Quiet presence").GetComponent<AudioSource>();
            source.clip = clip;
            source.pitch = 1;
            source.loop = false;
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        bool scenesPreserved = before == SceneState();
        if (!scenesPreserved)
        {
            throw new InvalidOperationException("Authored scene state changed.");
        }
        var report = new
        {
            task = "TASK-005",
            date = "2026-10-31",
            clip = AssetDatabase.GetAssetPath(clip),
            sameAsDiscovery = presentation.SqueakCount == 1 && presentation.Squeak(0) == presentation.Alarm,
            volume = presentation.SqueakVolume,
            idleSeconds = new[] { presentation.IdleSqueakInterval.x, presentation.IdleSqueakInterval.y },
            runSeconds = new[] { presentation.RunSqueakInterval.x, presentation.RunSqueakInterval.y },
            discoveryVolume = presentation.DetectionVolume,
            scenesPreserved,
            sceneSaved = false
        };
        Directory.CreateDirectory("Docs/Validation/ScorpionPaletteR11");
        File.WriteAllText("Docs/Validation/ScorpionPaletteR11/squeak-import.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static string SceneState() => string.Join("|", Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
    {
        var scene = SceneManager.GetSceneAt(index);
        return scene.handle + ":" + scene.path + ":" + scene.isDirty + ":" + scene.rootCount;
    }));
}
