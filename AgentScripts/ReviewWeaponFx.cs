using System;
using EmergencyShift;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class ReviewWeaponFx
{
    public static object Prepare()
    {
        var scene = EditorSceneManager.playModeStartScene;
        Directory.CreateDirectory("work/weapon-fx50");
        File.WriteAllText("work/weapon-fx50/play-start.txt", scene != null ? AssetDatabase.GetAssetPath(scene) : "");
        EditorSceneManager.playModeStartScene = null;
        return new { startScene = scene != null ? scene.name : "current scene" };
    }

    public static object Restore()
    {
        string path = File.ReadAllText("work/weapon-fx50/play-start.txt");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        return path;
    }

    public static object Begin(string folder)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Start Play before recording.");
        }
        var probe = new GameObject("Transient weapon FX comparison").AddComponent<WeaponFxReviewProbe>();
        probe.Begin(folder);
        return new { started = true, folder };
    }

    public static object Benchmark(string output)
    {
        var probe = new GameObject("Transient weapon FX benchmark").AddComponent<WeaponFxBenchmarkProbe>();
        probe.Begin(output);
        return output;
    }
}
