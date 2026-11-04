using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildPlayerHit
{
    public static async Task<object> Run()
    {
        // Return the Task to Pipeline before entering the synchronous native build.
        await Task.Yield();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            throw new InvalidOperationException("Requires an idle stopped Editor.");
        }
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            if (SceneManager.GetSceneAt(index).isDirty)
            {
                throw new InvalidOperationException("Preserve unsaved scenes before building.");
            }
        }
        if (EditorSceneManager.playModeStartScene != null)
        {
            throw new InvalidOperationException("Play must start from the open level.");
        }

        const string output = "Builds/PlayerHitTask009/Release/EmergencyShift.exe";
        const string evidence = "Docs/Validation/PlayerHitTask009/release-build.json";
        if (Directory.Exists(Path.GetDirectoryName(output)) || File.Exists(evidence))
        {
            throw new IOException("Preserve existing build output and evidence.");
        }
        string[] scenes =
        {
            "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity",
            "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity",
            "Assets/EmergencyShift/Scenes/BasementPrototype.unity",
            "Assets/EmergencyShift/Scenes/DoorCrushTest.unity"
        };
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.CompressWithLz4HC | BuildOptions.DetailedBuildReport
        });
        string managed = Path.Combine(Path.GetDirectoryName(output), "EmergencyShift_Data/Managed");
        string[] assemblies = Directory.Exists(managed)
            ? Directory.GetFiles(managed, "EmergencyShift*.dll").Select(Path.GetFileName).OrderBy(name => name).ToArray()
            : Array.Empty<string>();
        bool releaseOnly = !report.summary.options.HasFlag(BuildOptions.Development) &&
            assemblies.Contains("EmergencyShift.Runtime.dll") &&
            !assemblies.Any(name => name.Contains("Editor") || name.Contains("Tests") || name.Contains("Development"));
        string[] requiredAssets = Enumerable.Range(1, 4)
            .Select(index => "Assets/EmergencyShift/Audio/PlayerHit01/player-hit-" + index.ToString("00") + ".wav")
            .Concat(new[] { "Assets/EmergencyShift/Data/Audio/PlayerHitAudio.asset" }).ToArray();
        string[] packed = report.packedAssets.SelectMany(pack => pack.contents)
            .Select(item => item.sourceAssetPath).Distinct().ToArray();
        string[] missingAssets = requiredAssets.Where(path => !packed.Contains(path)).ToArray();
        var result = new
        {
            result = report.summary.result.ToString(),
            errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings,
            seconds = report.summary.totalTime.TotalSeconds,
            bytes = report.summary.totalSize,
            options = report.summary.options.ToString(),
            assemblies,
            releaseOnly,
            requiredAssets,
            missingAssets,
            scenes,
            output,
            scenesSavedByHelper = 0,
            settingsWrittenByHelper = false,
            limits = "Release compilation and asset inclusion from the shared working tree; artistic review and standalone gameplay are not inferred."
        };
        File.WriteAllText(evidence, JsonConvert.SerializeObject(result, Formatting.Indented));
        if (report.summary.result != BuildResult.Succeeded || !releaseOnly || missingAssets.Length != 0)
        {
            throw new InvalidOperationException("Release qualification failed; inspect saved evidence.");
        }
        return result;
    }

    public static object Messages()
    {
        var report = BuildReport.GetLatestReport();
        var messages = report.steps.SelectMany(step => step.messages)
            .Where(message => message.type == LogType.Warning || message.type == LogType.Error || message.type == LogType.Exception)
            .Select(message => new { type = message.type.ToString(), message.content }).ToArray();
        var result = new { result = report.summary.result.ToString(), messages,
            classification = "Two Pipeline HTTP main-thread timeout logs occurred during the synchronous build, not compiler/game failures. The warning reports intentionally disabled Pipeline in the ordinary Player." };
        File.WriteAllText("Docs/Validation/PlayerHitTask009/release-build-messages.json",
            JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }
}
