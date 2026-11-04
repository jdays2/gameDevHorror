using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit ordinary release qualification, only after the final changes/tests and game lease.
// No Build Settings, Player Settings, scene saves or automatic MainMenu entry changes.
public static class Task008Build
{
    public static object Release()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            throw new InvalidOperationException("Requires idle stopped Editor.");
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("Do not build over unsaved authored edits.");
            }
        }
        if (EditorSceneManager.playModeStartScene != null)
        {
            throw new InvalidOperationException("Ordinary Play must still start from the open level.");
        }
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(project, "Builds/CodeReadabilityTask008/Release/EmergencyShift.exe");
        string evidence = Path.Combine(project, "Docs/Validation/CodeReadabilityTask008/release-build.json");
        if (File.Exists(evidence) || Directory.Exists(Path.GetDirectoryName(output)))
        {
            throw new IOException("Preserve an existing build/evidence directory; inspect it before rerunning.");
        }
        var options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity",
                "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity",
                "Assets/EmergencyShift/Scenes/BasementPrototype.unity",
                "Assets/EmergencyShift/Scenes/DoorCrushTest.unity"
            },
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.CompressWithLz4HC | BuildOptions.DetailedBuildReport
        };
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(options);
        string managed = Path.Combine(Path.GetDirectoryName(output), "EmergencyShift_Data/Managed");
        string[] assemblies = Directory.Exists(managed)
            ? Directory.GetFiles(managed, "EmergencyShift*.dll").Select(Path.GetFileName).OrderBy(x => x).ToArray()
            : Array.Empty<string>();
        bool releaseOnly = !report.summary.options.HasFlag(BuildOptions.Development) &&
            assemblies.Contains("EmergencyShift.Runtime.dll") &&
            !assemblies.Any(name => name.Contains("Development") || name.Contains("Editor") || name.Contains("Tests"));
        const string profile = "Assets/EmergencyShift/Resources/PickupGlintProfile.asset";
        bool profileIncluded = report.packedAssets.Any(pack => pack.contents.Any(item => item.sourceAssetPath == profile));
        var result = new JObject
        {
            ["result"] = report.summary.result.ToString(),
            ["errors"] = report.summary.totalErrors,
            ["warnings"] = report.summary.totalWarnings,
            ["bytes"] = report.summary.totalSize,
            ["seconds"] = report.summary.totalTime.TotalSeconds,
            ["actualOptions"] = report.summary.options.ToString(),
            ["assemblies"] = JArray.FromObject(assemblies),
            ["releaseOnly"] = releaseOnly,
            ["existingPickupProfileIncluded"] = profileIncluded,
            ["scenes"] = JArray.FromObject(options.scenes),
            ["player"] = output,
            ["playerSha256"] = File.Exists(output) ? Hash(output) : null,
            ["settingsWrittenByHelper"] = false,
            ["scenesSavedByHelper"] = 0,
            ["limits"] = "Build inclusion and compilation only; no claim of gameplay/art acceptance or performance. Native scene/input preservation is compared separately after this operation."
        };
        File.WriteAllText(evidence, result.ToString());
        if (report.summary.result != BuildResult.Succeeded || !releaseOnly || !profileIncluded)
        {
            throw new InvalidOperationException("Release qualification failed; inspect the saved build report.");
        }
        return result;
    }

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (FileStream file = File.OpenRead(path))
        {
            return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
        }
    }
}
