using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Opt-in launcher for the actual existing diagnostic routines, not replacement gameplay.
// Invoke only under the game lease in disposable Play; Stop restores the authored scene.
public static class Task008Diagnostics
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<object> Smoke(string filename)
    {
        RequirePlay();
        string path = Output(filename);
        RefuseExisting(path);
        var host = new GameObject("Temporary TASK-008 smoke observer");
        UnityEngine.Object.DontDestroyOnLoad(host);
        var smoke = host.AddComponent<DevelopmentBuildSmoke>();
        typeof(DevelopmentBuildSmoke).GetField("output", PrivateInstance).SetValue(smoke, path);
        var routine = (IEnumerator)typeof(DevelopmentBuildSmoke).GetMethod("Run", PrivateInstance).Invoke(smoke, null);
        Exception failure = null;
        smoke.StartCoroutine(Observe(routine, exception => failure = exception));
        try
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 40;
            while (!File.Exists(path) && failure == null && Time.realtimeSinceStartupAsDouble < deadline)
            {
                RequirePlay();
                await Task.Delay(40);
            }
            if (!File.Exists(path))
            {
                File.WriteAllText(path, new JObject
                {
                    ["passed"] = false,
                    ["runnerFailure"] = failure != null ? failure.ToString() : "Actual smoke routine exceeded forty seconds."
                }.ToString());
            }
            return JObject.Parse(File.ReadAllText(path));
        }
        finally
        {
            if (host != null)
            {
                UnityEngine.Object.Destroy(host);
            }
        }
    }

    public static async Task<object> Sleeper(string filename, string enemyPath, string bulletPath, string visualsPath)
    {
        RequirePlay();
        string path = Output(filename);
        RefuseExisting(path);
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
        var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
        var visuals = AssetDatabase.LoadAssetAtPath<ShotVisualProfile>(visualsPath);
        if (enemy == null || projectile == null || visuals == null || projectile.GetComponent<BulletProjectile2D>() == null)
        {
            throw new InvalidOperationException("Use actual configured diagnostic prefab/visual assets.");
        }
        var host = new GameObject("Temporary TASK-008 Sleeper observer");
        var probe = host.AddComponent<SleeperRegressionProbe>();
        try
        {
            probe.Begin(enemy, projectile.GetComponent<BulletProjectile2D>(), visuals, path);
            double deadline = Time.realtimeSinceStartupAsDouble + 55;
            while (!File.Exists(path) && Time.realtimeSinceStartupAsDouble < deadline)
            {
                RequirePlay();
                await Task.Delay(40);
            }
            if (!File.Exists(path))
            {
                throw new TimeoutException("Actual Sleeper diagnostic did not finish in fifty-five seconds.");
            }
            return JObject.Parse(File.ReadAllText(path));
        }
        finally
        {
            if (host != null)
            {
                UnityEngine.Object.Destroy(host);
            }
        }
    }

    private static IEnumerator Observe(IEnumerator routine, Action<Exception> failed)
    {
        while (true)
        {
            object next;
            try
            {
                if (!routine.MoveNext())
                {
                    break;
                }
                next = routine.Current;
            }
            catch (Exception exception)
            {
                failed(exception);
                break;
            }
            yield return next;
        }
    }

    private static void RequirePlay()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires owned disposable Play; discard runtime staging on Stop.");
        }
    }

    private static string Output(string filename)
    {
        if (Path.GetFileName(filename) != filename || !filename.EndsWith(".json", StringComparison.Ordinal))
        {
            throw new ArgumentException("Use an evidence JSON filename.", nameof(filename));
        }
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs/Validation/CodeReadabilityTask008", filename));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        return path;
    }

    private static void RefuseExisting(string path)
    {
        if (File.Exists(path))
        {
            throw new IOException("Preserve existing diagnostic evidence: " + path);
        }
    }
}
