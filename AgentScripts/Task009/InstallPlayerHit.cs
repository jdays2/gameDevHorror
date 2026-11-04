// Execute only with game access. No scene save and no broad SaveAssets.
using System;
using System.IO;
using System.Linq;
using EmergencyShift.Features.PlayerHitAudio;
using EmergencyShift.Features.PlayerHitAudio.Config;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

public static class InstallPlayerHit
{
    private const string ProfilePath = "Assets/EmergencyShift/Data/Audio/PlayerHitAudio.asset";
    private const string PrefabPath = "Assets/EmergencyShift/Resources/ProjectContext.prefab";

    public static object Apply()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            throw new InvalidOperationException("Install into an idle stopped Editor.");
        }
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == PrefabPath && stage.scene.isDirty)
        {
            throw new InvalidOperationException("Preserve the owner's unsaved ProjectContext prefab stage.");
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var clips = new AudioClip[4];
        for (int index = 0; index < clips.Length; index++)
        {
            string path = "Assets/EmergencyShift/Audio/PlayerHit01/player-hit-" + (index + 1).ToString("D2") + ".wav";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
            clips[index] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clips[index] == null || clips[index].channels != 1 || clips[index].frequency != 48000)
            {
                throw new InvalidOperationException("Unexpected native hit clip import: " + path);
            }
        }

        PlayerHitAudioProfile profile = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PlayerHitAudioProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        using (var data = new SerializedObject(profile))
        {
            SerializedProperty bank = data.FindProperty("clips");
            bank.arraySize = clips.Length;
            for (int index = 0; index < clips.Length; index++)
            {
                bank.GetArrayElementAtIndex(index).objectReferenceValue = clips[index];
            }
            data.FindProperty("volume").floatValue = .8f;
            data.FindProperty("pitchVariation").floatValue = .012f;
            data.FindProperty("overlapFadeSeconds").floatValue = .018f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(profile);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int installerCount;
        try
        {
            PlayerHitAudioInstaller installer = root.GetComponent<PlayerHitAudioInstaller>();
            if (installer == null)
            {
                installer = root.AddComponent<PlayerHitAudioInstaller>();
            }
            using (var data = new SerializedObject(installer))
            {
                data.FindProperty("profile").objectReferenceValue = profile;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            using (var data = new SerializedObject(root.GetComponent<ProjectContext>()))
            {
                SerializedProperty installers = data.FindProperty("_monoInstallers");
                bool present = false;
                for (int index = 0; index < installers.arraySize; index++)
                {
                    present |= installers.GetArrayElementAtIndex(index).objectReferenceValue == installer;
                }
                if (!present)
                {
                    int index = installers.arraySize;
                    installers.arraySize++;
                    installers.GetArrayElementAtIndex(index).objectReferenceValue = installer;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                installerCount = installers.arraySize;
            }
            if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
            {
                throw new IOException("Could not save the scoped ProjectContext prefab change.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        var result = new { profile = ProfilePath, preset = "B: owner-selected direction; final in-game mix assessment pending",
            profile.Volume, profile.PitchVariation, profile.OverlapFadeSeconds, installerCount,
            clips = clips.Select(clip => new { path = AssetDatabase.GetAssetPath(clip), clip.channels,
                clip.frequency, clip.length, clip.loadState }).ToArray(), scenesSaved = false };
        Directory.CreateDirectory("Docs/Validation/PlayerHitTask009");
        File.WriteAllText("Docs/Validation/PlayerHitTask009/native-install.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }
}
