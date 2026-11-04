using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Opt-in launcher for the actual existing diagnostic routines, not replacement gameplay.
// Invoke only under the game lease in disposable Play; Stop restores the authored scene.
public static class Task008Diagnostics
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<object> ScorpionMotion(string filename)
    {
        RequirePlay();
        string path = Output(filename);
        RefuseExisting(path);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        LevelContext2D context = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            context = context != null ? context : root.GetComponentInChildren<LevelContext2D>();
            foreach (EnemyHealth2D enemy in root.GetComponentsInChildren<EnemyHealth2D>(true))
            {
                enemy.gameObject.SetActive(false);
            }
        }
        if (context == null || Time.timeScale != 1)
        {
            throw new InvalidOperationException("Requires one composed disposable running level at normal time.");
        }
        context.Player.SetHealthCapacity(999, 999);
        context.Player.GetComponent<PlayerMotor2D>().enabled = false;
        context.Player.GetComponent<PlayerAim2D>().enabled = false;
        var playerBody = context.Player.GetComponent<Rigidbody2D>();
        playerBody.constraints = RigidbodyConstraints2D.FreezeAll;
        Vector2 origin = new Vector2(1000, 1000);
        playerBody.position = origin + Vector2.up * 4;
        context.Player.transform.position = playerBody.position;
        context.Flashlight.SetPower(false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/Scorpion.prefab");
        var actor = context.CreateInstance(prefab, origin, Quaternion.identity).GetComponent<Scorpion2D>();
        var body = actor.GetComponent<Rigidbody2D>();
        var definition = new SerializedObject(actor).FindProperty("definition").objectReferenceValue;
        string definitionBefore = EditorJsonUtility.ToJson(definition);
        string presentationBefore = EditorJsonUtility.ToJson(actor.Presentation);
        var report = new JObject { ["passed"] = false, ["scene"] = scene.path };
        try
        {
            Physics2D.SyncTransforms();
            bool attacked = false;
            bool completed = false;
            bool gaitAdvanced = false;
            Vector2 heading = Vector2.zero;
            float pursuitSpeed = 0;
            float attackSpeed = 0;
            double deadline = Time.realtimeSinceStartupAsDouble + 6;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                RequirePlay();
                Vector2 velocity = body.linearVelocity;
                if (!float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                {
                    throw new InvalidOperationException("Real Rigidbody velocity became non-finite.");
                }
                gaitAdvanced |= actor.GaitPhase > .05f;
                if (actor.IsAttacking)
                {
                    if (!attacked)
                    {
                        heading = actor.AttackHeading;
                    }
                    attacked = true;
                    if (Vector2.Distance(heading, actor.AttackHeading) > .0001f)
                    {
                        throw new InvalidOperationException("An active attack changed its committed heading.");
                    }
                    attackSpeed = Mathf.Max(attackSpeed, velocity.magnitude);
                }
                else if (attacked)
                {
                    completed = true;
                    break;
                }
                else
                {
                    pursuitSpeed = Mathf.Max(pursuitSpeed, velocity.magnitude);
                }
                await Task.Delay(10);
            }
            float travelled = Vector2.Distance(body.position, origin);
            if (!completed || !gaitAdvanced || pursuitSpeed < .1f || attackSpeed < .1f || travelled < .5f)
            {
                throw new InvalidOperationException($"Real pursuit/attack controls failed: completed={completed}, gait={gaitAdvanced}, pursuit={pursuitSpeed}, attack={attackSpeed}, travel={travelled}.");
            }
            actor.enabled = false;
            if (body.linearVelocity.sqrMagnitude > .000001f)
            {
                throw new InvalidOperationException("Disable did not stop the real motor.");
            }
            actor.enabled = true;
            actor.ReceiveBulletHit(999, actor.transform.position, Vector2.right);
            if (actor.IsAlive || body.simulated || actor.GetComponent<CircleCollider2D>().enabled || body.linearVelocity.sqrMagnitude > .000001f)
            {
                throw new InvalidOperationException("Death did not stop living physics.");
            }
            if (EditorJsonUtility.ToJson(definition) != definitionBefore || EditorJsonUtility.ToJson(actor.Presentation) != presentationBefore)
            {
                throw new InvalidOperationException("Native play mutated shared authored tuning.");
            }
            report["passed"] = true;
            report["pursuitSpeedObserved"] = pursuitSpeed;
            report["attackSpeedObserved"] = attackSpeed;
            report["travelled"] = travelled;
            report["gaitAdvanced"] = gaitAdvanced;
            report["committedAttackCompleted"] = completed;
            report["disableAndDeathStoppedPhysics"] = true;
            report["sharedTuningUnchanged"] = true;
        }
        catch (Exception exception)
        {
            report["failure"] = exception.ToString();
        }
        finally
        {
            if (actor != null)
            {
                UnityEngine.Object.Destroy(actor.gameObject);
            }
        }
        File.WriteAllText(path, report.ToString());
        if (!(bool)report["passed"])
        {
            throw new InvalidOperationException("Native Scorpion motion failed; inspect " + path);
        }
        return report;
    }

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
