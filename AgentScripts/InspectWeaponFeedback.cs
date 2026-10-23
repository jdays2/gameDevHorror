using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyShift;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class InspectWeaponFeedback
{
    public static object RestorePreloadedInput()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before restoring Editor input preload state.");
        }
        var current = PlayerSettings.GetPreloadedAssets();
        if (current.Length != 0)
        {
            return new { restored = false, count = current.Length };
        }
        string path = AssetDatabase.GUIDToAssetPath("052faaac586de48259a63d0c4782560b");
        var input = AssetDatabase.LoadAllAssetsAtPath(path).Single(asset =>
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);
            return localId == -944628639613478452;
        });
        PlayerSettings.SetPreloadedAssets(new[] { input });
        AssetDatabase.SaveAssets();
        return new { restored = true, input = input.name };
    }

    public static object VerifyAudio()
    {
        var profile = AssetDatabase.LoadAssetAtPath<PlayerAudioSet>("Assets/EmergencyShift/Audio/PistolIndoor/PistolIndoorAudio.asset");
        profile.Validate();
        if (EditorUtility.IsDirty(profile) || profile.Impact(ImpactMaterial.Flesh).Count != 6 || Math.Abs(profile.FleshImpactVolume - .28f) > .001f)
        {
            throw new InvalidOperationException("Shared contact bank is dirty or incomplete.");
        }
        var names = new HashSet<string>();
        foreach (AudioClip clip in profile.Impact(ImpactMaterial.Flesh))
        {
            if (clip == null || !AssetDatabase.GetAssetPath(clip).Contains("/FleshContacts04/flesh-contact-B") || clip.length < .28f || clip.length > .351f || !names.Add(clip.name))
            {
                throw new InvalidOperationException("Expected six unique, short B contact variants.");
            }
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.compressionFormat != AudioCompressionFormat.PCM || !settings.preloadAudioData || clip.frequency != 48000)
            {
                throw new InvalidOperationException("Trial contact must preload PCM at its original 48 kHz.");
            }
        }
        if (!names.SetEquals(new[] { "flesh-contact-B", "flesh-contact-B-02", "flesh-contact-B-03", "flesh-contact-B-04", "flesh-contact-B-05", "flesh-contact-B-06" }))
        {
            throw new InvalidOperationException("Unexpected B variation bank members.");
        }
        return new { passed = true, profile.FleshImpactVolume, clips = names.ToArray(), sharedProfileClean = true };
    }
    public static object Begin(string output)
    {
        Object.FindAnyObjectByType<LevelContext2D>().gameObject.AddComponent<WeaponFeedbackSceneProbe>().Begin(output);
        return new { started = true };
    }
    public static object Check()
    {
        return new
        {
            scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => new { name = SceneManager.GetSceneAt(i).name, dirty = SceneManager.GetSceneAt(i).isDirty }).ToArray(),
            cameras = Object.FindObjectsByType<Camera>().Select(c => new { c.name, position = c.transform.position.ToString(), c.orthographicSize }).ToArray(),
            players = Object.FindObjectsByType<PlayerWeapon2D>().Select(w => new { w.name, position = w.transform.position.ToString(), json = EditorJsonUtility.ToJson(w), audio = EditorJsonUtility.ToJson(w.GetComponent<PlayerAudio2D>()) }).ToArray(),
            enemies = Object.FindObjectsByType<Damageable2D>().Select(e => new { e.name, position = e.transform.position.ToString(), type = e.GetType().Name, e.IsAlive }).ToArray()
        };
    }
}
