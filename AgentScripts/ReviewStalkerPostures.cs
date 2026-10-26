using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviewStalkerPostures
{
    private const string Folder = "Concepts/Used/Stalker/Postures04/";

    public static object Verify()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires stopped Editor.");
        }
        JObject baseline = JObject.Parse(File.ReadAllText("Concepts/Used/Stalker/Locomotion02/baseline-placements.json"));
        var rows = new List<object>();
        foreach (JToken row in baseline["rows"])
        {
            string path = (string)row["path"];
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            }
            try
            {
                var actors = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShadowStalker2D>(true)).ToArray();
                if (actors.Length != row["actors"].Count())
                {
                    throw new InvalidOperationException("Actor count changed: " + path);
                }
                foreach (JToken saved in row["actors"])
                {
                    ShadowStalker2D actor = actors.Single(value => Hierarchy(value.transform) == (string)saved["path"]);
                    var oldFields = JObject.Parse((string)saved["serialized"])["MonoBehaviour"] as JObject;
                    var newFields = JObject.Parse(EditorJsonUtility.ToJson(actor))["MonoBehaviour"] as JObject;
                    oldFields.Remove("m_EditorClassIdentifier");
                    newFields.Remove("m_EditorClassIdentifier");
                    if (!JToken.DeepEquals(oldFields,newFields) || actor.transform.position.ToString("R") != (string)saved["position"] ||
                        actor.transform.lossyScale.ToString("R") != (string)saved["scale"])
                    {
                        throw new InvalidOperationException("Authored actor changed: " + actor.name);
                    }
                    actor.GetComponent<StalkerSpriteAnimator>().SpriteSet.Validate();
                    if (actor.GetComponentsInChildren<MeshRenderer>(true).Length != 0 ||
                        GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(actor.gameObject) != 0)
                    {
                        throw new InvalidOperationException("Unexpected runtime mesh/missing script: " + actor.name);
                    }
                }
                rows.Add(new { path, count=actors.Length, scene.isDirty, unchanged=true, missingScripts=0, runtimeMeshes=0 });
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene,true);
                }
            }
        }
        var set = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
        bool runtimeHasIk = typeof(StalkerSpriteAnimator).Assembly.GetType("EmergencyShift.Features.Enemies.Logic.StalkerGaitModel") != null;
        if (runtimeHasIk)
        {
            throw new InvalidOperationException("Bake-only IK is still compiled into the runtime assembly.");
        }
        var textures = Enumerable.Range(0,set.Phases*set.ClosureRows).Select(index => set.Frame(index).texture).Distinct().ToArray();
        var result = new { scenes=rows, textures=textures.Select(texture => new { texture.name, texture.width, texture.height, format=texture.format.ToString() }),
            sharedTextureBytes=textures.Sum(texture => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture)),
            runtimeHasIk, playModeStartScene=EditorSceneManager.playModeStartScene,
            authoringGate="Not applicable: no scene objects added, moved or saved" };
        File.WriteAllText(Folder+"preservation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static object Build()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before building.");
        }
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] {"Assets/EmergencyShift/Scenes/MechanicsSandbox.unity","Assets/EmergencyShift/Scenes/MorgueRouteTest.unity"},
            locationPathName="Builds/StalkerPostures04/EmergencyShift.exe", target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.CompressWithLz4HC });
        var result = new { result=report.summary.result.ToString(), errors=report.summary.totalErrors,
            warnings=report.summary.totalWarnings, bytes=report.summary.totalSize,
            messages=report.steps.SelectMany(step => step.messages).Where(message => message.type==LogType.Warning || message.type==LogType.Error)
                .Select(message => new { level=message.type.ToString(),message.content }) };
        File.WriteAllText(Folder+"build-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Release build failed.");
        }
        return result;
    }

    private static string Hierarchy(Transform value) => value.parent==null ? value.name : Hierarchy(value.parent)+"/"+value.name;
}
