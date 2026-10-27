using System;
using System.IO;
using System.Linq;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Update only the existing prefab's claw channel; never save or rebuild scenes.</summary>
public static class ReviseScorpionAudio
{
    private const string PrefabPath = "Assets/EmergencyShift/Prefabs/Enemies/Scorpion.prefab";
    private const string ClipPath = "Assets/EmergencyShift/Audio/Enemies/Scorpion/Attack.wav";

    public static object Install()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Requires stopped Editor.");
        }
        AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceUpdate);
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform audio = prefab.transform.Find("Audio");
            var feedback = new SerializedObject(prefab.GetComponent<ScorpionAudio2D>());
            var voice = (AudioSource)feedback.FindProperty("voice").objectReferenceValue;
            voice.name = "Voice";
            Transform child = audio.Find("Claw grasp");
            AudioSource claws;
            if (child == null)
            {
                claws = new GameObject("Claw grasp", typeof(AudioSource)).GetComponent<AudioSource>();
                claws.transform.SetParent(audio, false);
            }
            else
            {
                claws = child.GetComponent<AudioSource>();
            }
            claws.playOnAwake = false;
            claws.loop = false;
            claws.spatialBlend = 0;
            claws.dopplerLevel = 0;
            claws.priority = voice.priority;
            feedback.FindProperty("claws").objectReferenceValue = claws;
            feedback.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        return Inspect();
    }

    public static object Inspect()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var presentation = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>("Assets/EmergencyShift/Data/Enemies/Scorpion/Presentation.asset");
        var serialized = new SerializedObject(prefab.GetComponent<ScorpionAudio2D>());
        var clips = prefab.GetComponentsInChildren<AudioSource>(true);
        string[] required = { "voice", "claws", "contact", "presence" };
        bool wired = required.All(key => serialized.FindProperty(key).objectReferenceValue != null);
        var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPath);
        var scene = SceneManager.GetActiveScene();
        var instances = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ScorpionAudio2D>(true)).ToArray();
        var report = new
        {
            wired,
            sources = clips.Length,
            distinct = required.Select(key => serialized.FindProperty(key).objectReferenceValue).Distinct().Count() == 4,
            clip = AssetDatabase.GetAssetPath(presentation.Grasp),
            guid = AssetDatabase.AssetPathToGUID(ClipPath),
            length = presentation.Grasp.length,
            channels = presentation.Grasp.channels,
            frequency = presentation.Grasp.frequency,
            importer.forceToMono,
            compression = importer.defaultSampleSettings.compressionFormat.ToString(),
            loadType = importer.defaultSampleSettings.loadType.ToString(),
            missingScripts = prefab.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            scene = scene.path,
            sceneDirty = scene.isDirty,
            inheritedInstances = instances.Select(instance => new
            {
                instance.name,
                clawsWired = new SerializedObject(instance).FindProperty("claws").objectReferenceValue != null,
                sources = instance.GetComponentsInChildren<AudioSource>(true).Length
            }).ToArray(),
            scenesSaved = false
        };
        Directory.CreateDirectory("Docs/Validation/ScorpionAudio02");
        File.WriteAllText("Docs/Validation/ScorpionAudio02/import.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }
}
