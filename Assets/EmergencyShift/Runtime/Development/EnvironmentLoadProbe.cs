#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Opt-in standalone measurement, never attached to a saved scene or included in release.</summary>
    public sealed class EnvironmentLoadProbe : MonoBehaviour
    {
        [Serializable]
        private struct FrameSample
        {
            public double frameMs;
            public double mainThreadMs;
            public double renderThreadMs;
            public long gcBytes;
            public long batches;
            public long drawCalls;
            public int poweredLights;
        }

        [Serializable]
        private sealed class Segment
        {
            public string view;
            public bool reserveEnabled;
            public int renderers;
            public List<FrameSample> frames = new List<FrameSample>(4096);
        }

        [Serializable]
        private sealed class Report
        {
            public string scene;
            public string unity;
            public string gpu;
            public string cpu;
            public string graphicsApi;
            public int width;
            public int height;
            public int vSync;
            public int targetFrameRate;
            public string failure;
            public bool mainThreadAvailable;
            public bool renderThreadAvailable;
            public bool gcAvailable;
            public bool batchesAvailable;
            public bool drawCallsAvailable;
            public float baseCamera;
            public float upgradedCamera;
            public float cappedCamera;
            public float restoredCamera;
            public bool powerKeepsZoom;
            public float corridorEndX;
            public float blockedPalletY;
            public int screenDarkFrames;
            public int screenLitFrames;
            public int emissionMismatches;
            public List<Segment> segments = new List<Segment>();
        }

        private string destination;
        private Report report;
        private ProfilerRecorder mainThread;
        private ProfilerRecorder renderThread;
        private ProfilerRecorder gc;
        private ProfilerRecorder batches;
        private ProfilerRecorder draws;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--environment-benchmark");
            if (!Debug.isDebugBuild || index < 0 || index + 1 >= args.Length)
            {
                return;
            }

            var probe = new GameObject("Temporary environment measurement").AddComponent<EnvironmentLoadProbe>();
            probe.destination = args[index + 1];
            probe.StartCoroutine(probe.Run());
        }

        private IEnumerator Run()
        {
            report = new Report
            {
                scene = SceneManager.GetActiveScene().name,
                unity = Application.unityVersion,
                gpu = SystemInfo.graphicsDeviceName,
                cpu = SystemInfo.processorType,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString()
            };
            Application.logMessageReceived += OnLog;
            if (report.scene != "EnvironmentNarrativeStressReview")
            {
                report.failure = "Expected the authored environment stress scene.";
                Finish();
                yield break;
            }

            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            yield return null;
            report.width = Screen.width;
            report.height = Screen.height;
            report.vSync = QualitySettings.vSyncCount;
            report.targetFrameRate = Application.targetFrameRate;
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
            gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            report.mainThreadAvailable = mainThread.Valid;
            report.renderThreadAvailable = renderThread.Valid;
            report.gcAvailable = gc.Valid;
            report.batchesAvailable = batches.Valid;
            report.drawCallsAvailable = draws.Valid;

            var player = FindAnyObjectByType<PlayerLife2D>();
            player.GetComponent<PlayerMotor2D>().enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            body.simulated = false;
            var flashlight = player.GetComponent<Flashlight2D>();
            var camera = Camera.main.GetComponent<CameraFollow2D>();
            body.position = new Vector2(1.4f, 1.2f);
            player.transform.position = body.position;
            camera.SnapToTarget();
            var upgrades = player.GetComponent<FlashlightUpgrades>();
            var originalModules = upgrades.Capture();
            int originalCount = upgrades.InstalledCount;
            report.baseCamera = Camera.main.orthographicSize;
            flashlight.SetPower(false);
            yield return null;
            report.powerKeepsZoom = Mathf.Abs(Camera.main.orthographicSize - report.baseCamera) < .01f;
            string message;
            upgrades.TryInstall("probe-lens", "Probe", .6f, 6, out message, true);
            yield return new WaitForSecondsRealtime(1.8f);
            report.upgradedCamera = Camera.main.orthographicSize;
            for (int i = 0; i < 12; i++)
            {
                upgrades.TryInstall("probe-lens", "Probe", .6f, 6, out message, true);
            }

            yield return new WaitForSecondsRealtime(1.8f);
            report.cappedCamera = Camera.main.orthographicSize;
            upgrades.Restore(originalModules, originalCount);
            yield return new WaitForSecondsRealtime(1.8f);
            report.restoredCamera = Camera.main.orthographicSize;
            if (Mathf.Abs(report.baseCamera - 4.6f) > .02f ||
                Mathf.Abs(report.upgradedCamera - 5.2f) > .02f ||
                Mathf.Abs(report.cappedCamera - 7.4f) > .02f ||
                Mathf.Abs(report.restoredCamera - 4.6f) > .02f || !report.powerKeepsZoom)
            {
                report.failure = "Flashlight framing progression/restoration failed.";
            }

            VerifyMovement(body);
            var terminal = GameObject.Find("Restarting terminal");
            var screen = terminal.GetComponentInChildren<LocalLamp2D>();
            var aperture = terminal.transform.Find("Emissive aperture").GetComponent<SpriteRenderer>();
            var reserves = new List<GameObject>();
            foreach (var transform in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (transform.name.StartsWith("LoadReserve /", StringComparison.Ordinal))
                {
                    reserves.Add(transform.gameObject);
                }
            }

            Vector2[] positions = { new Vector2(0, 2), new Vector2(12.4f, 2), new Vector2(28.2f, 1.8f), new Vector2(24, -4.8f) };
            string[] names = { "Receiving", "Laboratory", "Medical", "Staff" };
            var frameEnd = new WaitForEndOfFrame();
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var reserve in reserves)
                {
                    reserve.SetActive(pass == 0);
                }

                var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
                for (int view = 0; view < positions.Length; view++)
                {
                    body.position = positions[view];
                    player.transform.position = positions[view];
                    flashlight.SetPower(true);
                    flashlight.AimAt(positions[view] + Vector2.up * 3);
                    camera.SnapToTarget();
                    for (int warmup = 0; warmup < 45; warmup++)
                    {
                        yield return frameEnd;
                    }

                    var segment = new Segment
                    {
                        view = names[view],
                        reserveEnabled = pass == 0,
                        renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Length
                    };
                    report.segments.Add(segment);
                    float finishAt = Time.realtimeSinceStartup + 6.6f;
                    while (Time.realtimeSinceStartup < finishAt)
                    {
                        yield return frameEnd;
                        int activeLights = 0;
                        foreach (var light in lights)
                        {
                            if (light.enabled && light.intensity > .01f)
                            {
                                activeLights++;
                            }
                        }

                        if (segment.frames.Count < 4096)
                        {
                            bool lit = screen.CurrentIntensity > .01f;
                            if (lit)
                            {
                                report.screenLitFrames++;
                            }
                            else
                            {
                                report.screenDarkFrames++;
                            }

                            if (lit != aperture.enabled)
                            {
                                report.emissionMismatches++;
                            }

                            segment.frames.Add(new FrameSample
                            {
                                frameMs = Time.unscaledDeltaTime * 1000,
                                mainThreadMs = mainThread.Valid ? mainThread.LastValue / 1e6 : -1,
                                renderThreadMs = renderThread.Valid ? renderThread.LastValue / 1e6 : -1,
                                gcBytes = gc.Valid ? gc.LastValue : -1,
                                batches = batches.Valid ? batches.LastValue : -1,
                                drawCalls = draws.Valid ? draws.LastValue : -1,
                                poweredLights = activeLights
                            });
                        }
                    }
                }
            }

            if (report.screenDarkFrames == 0 || report.screenLitFrames == 0 || report.emissionMismatches != 0)
            {
                report.failure = "Screen emission did not follow its actual local light.";
            }

            Finish();
        }

        private void VerifyMovement(Rigidbody2D body)
        {
            var previous = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
            body.simulated = true;
            try
            {
                body.position = new Vector2(1.9f, -2);
                Physics2D.SyncTransforms();
                for (int i = 0; i < 400; i++)
                {
                    body.linearVelocity = Vector2.right * 3.5f;
                    Physics2D.Simulate(.02f);
                }

                report.corridorEndX = body.position.x;
                body.position = new Vector2(-4.05f, -2.8f);
                Physics2D.SyncTransforms();
                for (int i = 0; i < 50; i++)
                {
                    body.linearVelocity = Vector2.down * 3.5f;
                    Physics2D.Simulate(.02f);
                }

                report.blockedPalletY = body.position.y;
                if (report.corridorEndX < 29 || report.blockedPalletY < -4)
                {
                    report.failure = "Environment route or pallet collision regression.";
                }
            }
            finally
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
                Physics2D.simulationMode = previous;
            }
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

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            mainThread.Dispose();
            renderThread.Dispose();
            gc.Dispose();
            batches.Dispose();
            draws.Dispose();
        }
    }
}
#endif
