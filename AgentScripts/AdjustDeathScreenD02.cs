using System;
using System.IO;
using System.Linq;
using EmergencyShift.Features.DeathScreen.Config;
using EmergencyShift.Features.DeathScreen.View;
using EmergencyShift.Features.WorldAudio.Config;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Targeted prefab/profile revision; never saves or rebuilds an authored map.</summary>
public static class AdjustDeathScreenD02
{
    private const string Prefab = "Assets/EmergencyShift/Prefabs/UI/DeathScreenUI.prefab";
    private const string Profile = "Assets/EmergencyShift/Data/UI/DeathScreen/Transition.asset";

    public static object State()
    {
        Scene scene = SceneManager.GetActiveScene();
        var audio = AssetDatabase.LoadAssetAtPath<WorldAudioProfile>("Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset");
        bool hasGain = audio.WorldOutput.audioMixer.GetFloat(audio.AttenuationParameter, out float gainDb);
        int probes = scene.GetRootGameObjects().Count(root => root.name.StartsWith("TASK006 D02 temporary"));
        var result = new { scene = scene.path, scene.isDirty, sceneCount = SceneManager.sceneCount,
            playing = Application.isPlaying, startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            timeScale = Time.timeScale, listenerPause = AudioListener.pause, listenerVolume = AudioListener.volume,
            mixerGainDb = gainDb, probes };
        if (scene.isDirty || SceneManager.sceneCount != 1 || Application.isPlaying ||
            EditorSceneManager.playModeStartScene != null || Time.timeScale != 1 || AudioListener.pause ||
            AudioListener.volume != 1 || !hasGain || Mathf.Abs(gainDb) > .01f || probes != 0)
        {
            throw new InvalidOperationException("Editor state not restored: " + JsonConvert.SerializeObject(result));
        }
        File.WriteAllText("Docs/Validation/DeathScreenD02/final-state.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Use the acquired game lease and a stopped compiled Editor.");
        }
        string originalGuid = AssetDatabase.AssetPathToGUID(Prefab);
        var profile = AssetDatabase.LoadAssetAtPath<DeathScreenProfile>(Profile);
        var data = new SerializedObject(profile);
        data.FindProperty("worldFadeSeconds").floatValue = .32f;
        data.FindProperty("contentDelaySeconds").floatValue = .32f;
        data.FindProperty("contentFadeSeconds").floatValue = 0;
        data.FindProperty("irisFeather").floatValue = .18f;
        data.FindProperty("worldPauseDelaySeconds").floatValue = 1.4f;
        data.ApplyModifiedPropertiesWithoutUndo();
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(profile);
        GameObject root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            Transform[] before = root.GetComponentsInChildren<Transform>(true);
            var positions = before.Select(item => new { item, item.localPosition, item.localRotation, item.localScale }).ToArray();
            CanvasGroup shade = root.GetComponentsInChildren<CanvasGroup>(true).Single(item => item.name == "World Veil");
            UnityEngine.UI.Image image = shade.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Object.DestroyImmediate(image);
            }
            DeathScreenIris iris = shade.GetComponent<DeathScreenIris>();
            if (iris == null)
            {
                iris = shade.gameObject.AddComponent<DeathScreenIris>();
            }
            iris.color = Color.black;
            iris.raycastTarget = false;
            DeathScreenView view = root.GetComponentInChildren<DeathScreenView>(true);
            var viewData = new SerializedObject(view);
            viewData.FindProperty("iris").objectReferenceValue = iris;
            viewData.ApplyModifiedPropertiesWithoutUndo();
            if (positions.Any(item => item.item.localPosition != item.localPosition ||
                item.item.localRotation != item.localRotation || item.item.localScale != item.localScale) ||
                root.GetComponentsInChildren<Transform>(true).Length != before.Length)
            {
                throw new InvalidOperationException("The transition revision must preserve the complete UI hierarchy and transforms.");
            }
            PrefabUtility.SaveAsPrefabAsset(root, Prefab, out bool saved);
            if (!saved || AssetDatabase.AssetPathToGUID(Prefab) != originalGuid)
            {
                throw new InvalidOperationException("Prefab save or GUID preservation failed.");
            }
            var result = new { prefab = Prefab, profile = Profile, originalGuid, hierarchyPreserved = true,
                rootObjects = before.Length, authoredScenesSaved = 0, closureSeconds = .32f,
                contentFadeSeconds = 0, audioSources = root.GetComponentsInChildren<AudioSource>(true).Length };
            Directory.CreateDirectory("Docs/Validation/DeathScreenD02");
            File.WriteAllText("Docs/Validation/DeathScreenD02/authoring.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
