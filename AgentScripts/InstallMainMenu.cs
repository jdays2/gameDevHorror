using System;
using System.IO;
using System.Linq;
using EmergencyShift.Editor;
using EmergencyShift.Features.MainMenu.Config;
using EmergencyShift.Features.MainMenu.View;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class InstallMainMenu
{
    public static object Apply()
    {
        MainMenuAuthoring.Create();
        return Validate();
    }

    public static object Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuAuthoring.PrefabPath);
        var missing = prefab.GetComponentsInChildren<Component>(true).Count(component => component == null);
        var catalog = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MainMenuAuthoring.CatalogPath).ToCatalog();
        var style = AssetDatabase.LoadAssetAtPath<MainMenuStyle>(MainMenuAuthoring.StylePath);
        style.Validate();
        if (missing != 0 || EditorBuildSettings.scenes[0].path != MainMenuAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Invalid menu composition or build entry.");
        }

        for (int index = 0; index < catalog.Count; index++)
        {
            string path = catalog.At(index).Path;
            if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path))
            {
                throw new InvalidOperationException("Catalog scene missing from build: " + path);
            }
        }

        return new { missingScripts = missing, catalogScenes = catalog.Count,
            presenters = prefab.GetComponentsInChildren<MainMenuPresenter>(true).Length,
            buildEntry = EditorBuildSettings.scenes[0].path };
    }

    public static object Build()
    {
        Directory.CreateDirectory("Builds/MainMenuValidation");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = "Builds/MainMenuValidation/EmergencyShift.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        var result = new { result = report.summary.result.ToString(), errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
            seconds = report.summary.totalTime.TotalSeconds, output = report.summary.outputPath };
        Directory.CreateDirectory("work/main-menu");
        File.WriteAllText("work/main-menu/build.json", Newtonsoft.Json.JsonConvert.SerializeObject(result));
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Main menu player build failed.");
        }
        return result;
    }

    public static object InspectBuild()
    {
        BuildReport report = BuildReport.GetLatestReport();
        var messages = report.steps.SelectMany(step => step.messages)
            .Where(message => message.type == LogType.Error || message.type == LogType.Warning)
            .Select(message => new { type = message.type.ToString(), message.content }).ToArray();
        File.WriteAllText("work/main-menu/build-messages.json", Newtonsoft.Json.JsonConvert.SerializeObject(messages, Newtonsoft.Json.Formatting.Indented));
        return messages;
    }
}
