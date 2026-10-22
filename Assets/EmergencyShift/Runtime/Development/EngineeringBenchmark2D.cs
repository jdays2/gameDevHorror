#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using Zenject;
using Debug = UnityEngine.Debug;

namespace EmergencyShift
{
    /// <summary>Opt-in native-player baseline for pursuit and simultaneous death. Never saved in a scene.</summary>
    public sealed class EngineeringBenchmark2D : MonoBehaviour
    {
        [Serializable]
        private sealed class Sample
        {
            public double frameMs;
            public double aiMs;
            public double separationMs;
            public long gcBytes;
        }

        [Serializable]
        private sealed class Report
        {
            public string unity;
            public string failure;
            public int actors;
            public double spawnMs;
            public double deathDispatchMs;
            public int pools;
            public int settledParts;
            public int activeBrains;
            public int activeAudio;
            public int activeAnimators;
            public int movingBodies;
            public int wallPenetrations;
            public int peakPathBuilds;
            public List<Sample> pursuit = new List<Sample>();
            public List<Sample> death = new List<Sample>();
        }

        private readonly List<ShadowStalker2D> actors = new List<ShadowStalker2D>();
        private ProfilerRecorder ai;
        private ProfilerRecorder separation;
        private ProfilerRecorder allocations;
        private string destination;
        private NativeBenchmarkSettingsLease measurementSettings;
        private Report report;
        private BoxCollider2D barrier;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "--engineering-benchmark");
            if (!Debug.isDebugBuild || flag < 0 || flag + 2 >= args.Length)
            {
                return;
            }

            var runner = new GameObject("Temporary engineering benchmark").AddComponent<EngineeringBenchmark2D>();
            runner.destination = args[flag + 1];
            runner.report = new Report
            {
                actors = int.Parse(args[flag + 2]),
                unity = Application.unityVersion
            };
            FindAnyObjectByType<LevelContext2D>().InjectScoped(runner);
            runner.StartCoroutine(runner.Run());
        }

        private NavigationWorkBudget2D navigationBudget;

        [Inject]
        private void Construct(NavigationWorkBudget2D navigation) => navigationBudget = navigation;

        private IEnumerator Run()
        {
            Application.logMessageReceived += OnLog;
            measurementSettings = new NativeBenchmarkSettingsLease();
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            yield return null;
            UnityEngine.Random.InitState(20261015);
            ShadowStalker2D template = FindAnyObjectByType<ShadowStalker2D>();
            PlayerLife2D player = FindAnyObjectByType<PlayerLife2D>();
            foreach (var enemy in FindObjectsByType<ShadowStalker2D>())
            {
                enemy.gameObject.SetActive(false);
            }

            player.GetComponent<PlayerMotor2D>().enabled = false;
            player.GetComponent<PlayerInteraction>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            player.transform.position = new Vector3(100, 100, 0);
            player.GetComponent<Flashlight2D>().SetPower(false);
            Camera.main.GetComponent<CameraFollow2D>().enabled = false;
            Camera.main.transform.position = new Vector3(98, 100, -10);
            var wall = new GameObject("Benchmark wall", typeof(BoxCollider2D));
            wall.layer = 8;
            wall.transform.position = new Vector3(98, 100, 0);
            barrier = wall.GetComponent<BoxCollider2D>();
            barrier.size = new Vector2(.25f, 5);
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < report.actors; i++)
            {
                Vector2 position = new Vector2(93 + (i % 4) * .8f, 97 + (i / 4) * 1.05f);
                var actor = FindAnyObjectByType<LevelContext2D>().CreateInstance(template.gameObject, position, Quaternion.identity).GetComponent<ShadowStalker2D>();
                actor.gameObject.SetActive(true);
                actor.AttacksEnabled = false;
                actor.ApplyBulletHit(1, position, Vector2.right);
                actors.Add(actor);
            }

            watch.Stop();
            report.spawnMs = watch.Elapsed.TotalMilliseconds;
            Physics2D.SyncTransforms();
            ai = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "EmergencyShift.Stalker.Tick");
            separation = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "EmergencyShift.Crowd.Separation");
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            yield return new WaitForSeconds(1);
            float end = Time.time + 4;
            while (Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                report.pursuit.Add(Capture());
                foreach (var actor in actors)
                {
                    if (actor.GetComponent<Collider2D>().Distance(barrier).distance < -.04f)
                    {
                        report.wallPenetrations++;
                    }
                }
            }

            report.peakPathBuilds = navigationBudget.PeakBuilds;
            watch.Restart();
            foreach (var actor in actors)
            {
                actor.Kill();
            }

            watch.Stop();
            report.deathDispatchMs = watch.Elapsed.TotalMilliseconds;
            end = Time.time + 4;
            while (Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                report.death.Add(Capture());
            }

            foreach (var actor in actors)
            {
                if (actor.isActiveAndEnabled)
                {
                    report.activeBrains++;
                }

                if (actor.GetComponent<StalkerAudio2D>().isActiveAndEnabled)
                {
                    report.activeAudio++;
                }

                if (actor.GetComponent<StalkerSpriteAnimator>().isActiveAndEnabled)
                {
                    report.activeAnimators++;
                }

                if (actor.GetComponent<Rigidbody2D>().simulated)
                {
                    report.movingBodies++;
                }
            }

            foreach (var pool in FindObjectsByType<BloodPool2D>())
            {
                if (pool.transform.position.x > 80)
                {
                    report.pools++;
                }
            }

            foreach (var part in FindObjectsByType<GibMotion2D>())
            {
                if (part.transform.position.x > 80 && part.Settled)
                {
                    report.settledParts++;
                }
            }

            Finish();
        }

        private Sample Capture()
        {
            return new Sample
            {
                frameMs = Time.unscaledDeltaTime * 1000,
                aiMs = ai.Valid ? ai.LastValue / 1e6 : -1,
                separationMs = separation.Valid ? separation.LastValue / 1e6 : -1,
                gcBytes = allocations.Valid ? allocations.LastValue : -1
            };
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                report.failure = message;
            }
        }

        private void Finish()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(destination, JsonUtility.ToJson(report, true));
            Application.Quit(string.IsNullOrEmpty(report.failure) ? 0 : 2);
        }

        private void OnDisable() => ReleaseMeasurement();
        private void OnDestroy() => ReleaseMeasurement();

        private void ReleaseMeasurement()
        {
            StopAllCoroutines();
            measurementSettings?.Dispose();
            measurementSettings = null;
            Application.logMessageReceived -= OnLog;
            ai.Dispose();
            separation.Dispose();
            allocations.Dispose();
        }
    }
}
#endif
