using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildScorpionSmoke
{
    public static object Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Build requires stopped compiled Editor.");
        }
        Directory.CreateDirectory("Builds/ScorpionRoom01");
        var options = new BuildPlayerOptions {
            scenes = new[]{"Assets/EmergencyShift/Scenes/MechanicsSandbox.unity"},
            locationPathName = "Builds/ScorpionRoom01/EmergencyShift.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport build = BuildPipeline.BuildPlayer(options);
        var report = new {result=build.summary.result.ToString(),errors=build.summary.totalErrors,warnings=build.summary.totalWarnings,
            bytes=build.summary.totalSize,seconds=build.summary.totalTime.TotalSeconds,
            player=options.locationPathName, development=false, settingsMutated=false};
        File.WriteAllText("Docs/Validation/ScorpionRoom01/build.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return report;
    }

    public static object Details()
    {
        var method = typeof(BuildReport).GetMethod("GetLatestReport", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var report = method != null ? method.Invoke(null,null) as BuildReport : null;
        if (report == null)
        {
            return new { retainedReport = false };
        }
        var details = new { result=report.summary.result.ToString(), errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,
            messages=report.steps.SelectMany(step => step.messages).Where(message => message.type != UnityEngine.LogType.Log).Select(message => new {type=message.type.ToString(),message.content}).ToArray()};
        File.WriteAllText("Docs/Validation/ScorpionRoom01/build-details.json",JsonConvert.SerializeObject(details,Formatting.Indented));
        return details;
    }
}
