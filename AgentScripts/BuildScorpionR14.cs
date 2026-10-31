using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildScorpionR14
{
    public static object Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Build requires stopped compiled Editor.");
        }
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity" },
            locationPathName = "Builds/ScorpionRoom01/EmergencyShift.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport build = BuildPipeline.BuildPlayer(options);
        var report = new
        {
            task = "TASK-005",
            result = build.summary.result.ToString(),
            errors = build.summary.totalErrors,
            warnings = build.summary.totalWarnings,
            bytes = build.summary.totalSize,
            seconds = build.summary.totalTime.TotalSeconds,
            player = options.locationPathName,
            development = false,
            settingsMutated = false,
            messages = build.steps.SelectMany(step => step.messages)
                .Where(message => message.type != UnityEngine.LogType.Log)
                .Select(message => new { type = message.type.ToString(), message.content }).ToArray()
        };
        File.WriteAllText("Docs/Validation/ScorpionFeedbackR14/build.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        if (build.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("R14 release build failed; see saved report.");
        }
        return report;
    }
}
