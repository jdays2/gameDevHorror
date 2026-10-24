using System;
using EmergencyShift;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Threading.Tasks;
using EmergencyShift.Editor;

public static class ReviewWeaponFx
{
    public static object Prepare()
    {
        MainMenuEntryPolicy.Synchronize();
        return new { startScene = "MainMenu", route = "MorgueRouteTest via menu" };
    }

    public static object Restore()
    {
        MainMenuEntryPolicy.RestoreEntry();
        return MainMenuAuthoring.ScenePath;
    }

    public static async Task<object> Begin(string folder)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Start Play before recording.");
        }
        await MainMenuEditorNavigation.LaunchScene("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity");
        var probe = new GameObject("Transient weapon FX comparison").AddComponent<WeaponFxReviewProbe>();
        probe.Begin(folder);
        return new { started = true, folder };
    }

    public static async Task<object> Benchmark(string output)
    {
        await MainMenuEditorNavigation.LaunchScene("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity");
        var probe = new GameObject("Transient weapon FX benchmark").AddComponent<WeaponFxBenchmarkProbe>();
        probe.Begin(output);
        return output;
    }
}
