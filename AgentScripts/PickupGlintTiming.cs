using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift.Features.Pickups.Config;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PickupGlintTiming
{
    private const string Folder = "Docs/Validation/PickupGlintTiming58";
    private const string StartSceneFile = "work/PickupGlintTiming58-play-start.txt";

    public static object Apply()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Apply presentation tuning with Play stopped.");
        }

        const string path = "Assets/EmergencyShift/Resources/PickupGlintProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<PickupGlintProfile>(path);
        if (profile == null)
        {
            throw new InvalidOperationException("Pickup glint profile is missing.");
        }

        using (var serialized = new SerializedObject(profile))
        {
            serialized.FindProperty("duration").floatValue = 1;
            serialized.FindProperty("repeatPause").floatValue = .8f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(profile);
        var result = new
        {
            path,
            guid = AssetDatabase.AssetPathToGUID(path),
            duration = profile.Duration,
            repeatPause = profile.RepeatPause,
            intensity = profile.Intensity,
            stripeWidth = profile.StripeWidth,
            material = AssetDatabase.GetAssetPath(profile.Material),
            sceneSaved = false
        };
        Write("profile.json", result);
        return result;
    }

    public static object VerifyEdit()
    {
        Type type = FindType("EmergencyShift.Tests.PickupGlintTests");
        object fixture = Activator.CreateInstance(type);
        var passed = new List<string>();
        foreach (MethodInfo method in type.GetMethods().Where(method => method.GetCustomAttributes(false)
            .Any(attribute => attribute.GetType().FullName == "NUnit.Framework.TestAttribute")))
        {
            method.Invoke(fixture, null);
            passed.Add(method.Name);
        }
        if (passed.Count != 7)
        {
            throw new InvalidOperationException("Expected all seven pickup glint checks.");
        }
        var result = new { passed = passed.Count, methods = passed, execution = "Direct invocation of NUnit fixture assertions in the live Editor; Test Runner scene-save dialog was cancelled." };
        Write("edit-checks.json", result);
        return result;
    }

    public static object SelectTestScene()
    {
        RequireIdleTestRunner();
        string previous = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
        File.WriteAllText(StartSceneFile, previous);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/EmergencyShift/Scenes/MechanicsSandbox.unity");
        return new { previous, sceneSaved = false };
    }

    public static object RestoreTestScene()
    {
        string previous = File.ReadAllText(StartSceneFile);
        string current = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
        if (current != previous && current != "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity")
        {
            throw new InvalidOperationException("Another task changed the Play start scene; preserve its selection.");
        }
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        return new { restored = previous, sceneSaved = false };
    }

    public static async Task<object> VerifyPlay()
    {
        RequireIdleTestRunner();
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Verify in a disposable Play session.");
        }
        Type type = FindType("EmergencyShift.Tests.PickupGlintSceneTests");
        object fixture = Activator.CreateInstance(type);
        var scenario = (IEnumerator)type.GetMethod("BeamObstaclesInventoryChangesAndCleanupUseTheRealPickupPath").Invoke(fixture, null);
        var cleanup = (IEnumerator)type.GetMethod("Cleanup").Invoke(fixture, null);
        var host = new GameObject("Temporary pickup timing verification").AddComponent<PickupGlintTimingHost>();
        try
        {
            await host.Run(scenario, cleanup);
            var result = new { passed = true, execution = "Existing PlayMode fixture executed by a native coroutine in Play, including teardown; scenes never saved.",
                checks = "Held beam pause/repeat; quick aim away/back; power off/on; wall; inventory acceptance; partial ammo; pause; disable; immutable profile." };
            Write("play-checks.json", result);
            return result;
        }
        finally
        {
            UnityEngine.Object.Destroy(host.gameObject);
        }
    }

    private static Type FindType(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).FirstOrDefault(type => type != null)
            ?? throw new InvalidOperationException("Test fixture is not loaded: " + name);
    }

    private static void RequireIdleTestRunner()
    {
        if (File.Exists("Temp/pipeline_test_request.json"))
        {
            throw new InvalidOperationException("Wait for the existing Test Runner request before using the shared Editor.");
        }
    }

    private static void Write(string name, object result)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/" + name, JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}

public sealed class PickupGlintTimingHost : MonoBehaviour
{
    public Task Run(IEnumerator scenario, IEnumerator cleanup)
    {
        var completion = new TaskCompletionSource<bool>();
        StartCoroutine(Execute(scenario, cleanup, completion));
        return completion.Task;
    }

    private static IEnumerator Execute(IEnumerator scenario, IEnumerator cleanup, TaskCompletionSource<bool> completion)
    {
        Exception failure = null;
        foreach (IEnumerator routine in new[] { scenario, cleanup })
        {
            while (true)
            {
                object yielded;
                try
                {
                    if (!routine.MoveNext())
                    {
                        break;
                    }
                    yielded = routine.Current;
                }
                catch (Exception exception)
                {
                    failure = failure ?? exception;
                    break;
                }
                yield return yielded;
            }
        }
        if (failure != null)
        {
            completion.SetException(failure);
        }
        else
        {
            completion.SetResult(true);
        }
    }
}
