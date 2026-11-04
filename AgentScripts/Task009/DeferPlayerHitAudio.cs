using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.PlayerHitAudio.Config;
using EmergencyShift.Features.PlayerHitAudio.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DeferPlayerHitAudio
{
    private const string ProfilePath = "Assets/EmergencyShift/Data/Audio/PlayerHitAudio.asset";
    private const string Evidence = "Docs/Validation/PlayerHitTask009/Deferred-off/";

    public static object Apply()
    {
        RequireStopped();
        PlayerHitAudioProfile profile = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>(ProfilePath);
        string before = JsonUtility.ToJson(profile);
        using (var data = new SerializedObject(profile))
        {
            data.FindProperty("volume").floatValue = 0;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(profile);
        var result = new { profile = ProfilePath, before, after = JsonUtility.ToJson(profile),
            volume = profile.Volume, clipCount = profile.Clips.Count,
            guid = AssetDatabase.AssetPathToGUID(ProfilePath),
            scope = "Owner deferred TASK-009. Only this profile volume is muted; no new sound or character voice is approved." };
        Save("profile-change.json", result);
        return result;
    }

    public static async Task<object> Verify()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires ordinary Play for the accepted-damage mute check.");
        }
        Scene scene = SceneManager.GetActiveScene();
        LevelContext2D context = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        PlayerLife2D player = context.Player;
        foreach (EnemyHealth2D enemy in scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            enemy.gameObject.SetActive(false);
        }
        player.SetHealthCapacity(100, 100);
        await Task.Delay(800);
        PlayerHitAudioPresenter presenter = player.GetComponent<PlayerHitAudioPresenter>();
        int cuesBefore = presenter.PlayedCount;
        int healthBefore = player.Health;
        bool accepted = player.TryTakeDamage(1, player.transform.position, Vector2.right);
        await Task.Delay(30);
        Transform voiceRoot = player.transform.Find("Player hit voices (runtime)");
        AudioSource[] voices = voiceRoot.GetComponents<AudioSource>();
        PlayerHitAudioProfile profile = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>(ProfilePath);
        bool passed = accepted && player.Health == healthBefore - 1 &&
            presenter.PlayedCount == cuesBefore + 1 && profile.Volume == 0 &&
            voices.Length == 2 && voices.All(voice => voice.volume == 0);
        var result = new { passed, accepted, healthBefore, healthAfter = player.Health,
            responseEvents = presenter.PlayedCount - cuesBefore, profileVolume = profile.Volume,
            voices = voices.Select(voice => new { voice.volume, voice.isPlaying,
                clip = voice.clip != null ? voice.clip.name : null }).ToArray(),
            scope = "A real accepted damage command still updates health/presentation, but all player-hurt source gains are zero. Other enemy instances disabled only in Play; Stop restores the fixture." };
        Save("accepted-hit-muted.json", result);
        if (!passed)
        {
            throw new InvalidOperationException("Deferred player-hurt mute verification failed.");
        }
        return result;
    }

    public static object StoppedState()
    {
        RequireStopped();
        Scene scene = SceneManager.GetActiveScene();
        PlayerHitAudioProfile profile = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>(ProfilePath);
        var result = new { scene.path, scene.isDirty, sceneCount = SceneManager.sceneCount,
            objects = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length),
            startScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            Time.timeScale, Time.captureFramerate, AudioListener.volume, AudioListener.pause,
            profileVolume = profile.Volume,
            temporaryClips = Resources.FindObjectsOfTypeAll<AudioClip>().Count(clip => clip.name.StartsWith("TASK009 audition ")),
            runtimePresenters = Resources.FindObjectsOfTypeAll<PlayerHitAudioPresenter>().Length };
        Save("editor-stopped.json", result);
        return result;
    }

    private static void RequireStopped()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            throw new InvalidOperationException("Requires an idle stopped Editor.");
        }
    }

    private static void Save(string name, object result)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + name, JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
