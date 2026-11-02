using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildDeathAudio53
{
    public static object Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
        {
            throw new InvalidOperationException("Requires an idle EditMode Editor.");
        }
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            if (SceneManager.GetSceneAt(index).isDirty)
            {
                throw new InvalidOperationException("Unsaved scene edits must be preserved; this build never saves scenes.");
            }
        }
        string folder = Path.GetFullPath("../../work/bug053/build-release");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity", "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity", "Assets/EmergencyShift/Scenes/BasementPrototype.unity" },
            locationPathName = Path.Combine(folder, "EmergencyShift.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CompressWithLz4HC
        });
        string managed = Path.Combine(folder, "EmergencyShift_Data/Managed");
        string[] projectAssemblies = Directory.Exists(managed)
            ? Directory.GetFiles(managed, "EmergencyShift*.dll").Select(Path.GetFileName).ToArray()
            : Array.Empty<string>();
        var result = new
        {
            result = report.summary.result.ToString(), report.summary.totalErrors, report.summary.totalWarnings,
            report.summary.totalSize, report.summary.totalTime, projectAssemblies,
            development = EditorUserBuildSettings.development,
            scope = "Normal Windows64 build of the current shared working copy, including pre-existing local drafts; no publication",
            messages = report.steps.SelectMany(step => step.messages).Where(message => message.type == LogType.Error || message.type == LogType.Warning)
                .Select(message => new { message.type, message.content }).ToArray()
        };
        Directory.CreateDirectory("Docs/Validation/DeathAudio53");
        File.WriteAllText("Docs/Validation/DeathAudio53/build.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        if (report.summary.result != BuildResult.Succeeded || projectAssemblies.Any(name => name.Contains("Development") || name.Contains("Tests") || name.Contains("Editor")))
        {
            throw new InvalidOperationException("World audio release verification failed; inspect build.json.");
        }
        return result;
    }
}
