#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Opt-in native comparison. Runtime copies and profiling data never enter the saved scene.</summary>
    public sealed class FurnitureLightBenchmark : MonoBehaviour
    {
        private const int Repetitions = 6;
        private const float SampleSeconds = 1.2f;
        private const float WarmupSeconds = .35f;
        private static readonly int[,] Orders = { { 0, 1, 2 }, { 1, 2, 0 }, { 2, 0, 1 }, { 2, 1, 0 }, { 1, 0, 2 }, { 0, 2, 1 } };

        [Serializable]
        private struct FrameSample
        {
            public double frameMs;
            public double mainMs;
            public double renderMs;
            public double gpuMs;
            public double timingCpuMs;
            public double presentWaitMs;
            public long gcBytes;
            public long batches;
            public long draws;
            public bool timingFresh;
        }

        [Serializable]
        private sealed class Segment
        {
            public string scenario;
            public int repetition;
            public int variant;
            public int order;
            public bool ceiling;
            public bool shadows;
            public int activeFurniture;
            public List<FrameSample> frames = new List<FrameSample>(4096);
        }

        [Serializable]
        private sealed class Report
        {
            public string unity;
            public string gpu;
            public string cpu;
            public string api;
            public string failure;
            public int width;
            public int height;
            public int vSync;
            public int targetFrameRate;
            public int repetitions;
            public float sampleSeconds;
            public bool frameTimingEnabled;
            public bool mainRecorder;
            public bool renderRecorder;
            public bool gcRecorder;
            public bool drawRecorder;
            public bool batchRecorder;
            public List<Segment> segments = new List<Segment>();
        }

        private string destination;
        private NativeBenchmarkSettingsLease measurementSettings;
        private InputDevice keyboard;
        private InputDevice mouse;
        private bool keyboardWasEnabled;
        private bool mouseWasEnabled;
        private FurnitureLightStudy2D.Sample[] originalSamples;
        private Report report;
        private ProfilerRecorder main;
        private ProfilerRecorder render;
        private ProfilerRecorder gc;
        private ProfilerRecorder batches;
        private ProfilerRecorder draws;
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private ulong previousTiming;
        private readonly List<GameObject> copies = new List<GameObject>();
        private FurnitureLightStudy2D study;
        private Flashlight2D flashlight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--furniture-benchmark");
            if (!Debug.isDebugBuild || index < 0 || index + 1 >= args.Length)
            {
                return;
            }

            var probe = new GameObject("Temporary furniture benchmark").AddComponent<FurnitureLightBenchmark>();
            probe.destination = args[index + 1];
            probe.StartCoroutine(probe.Run());
        }

        private IEnumerator Run()
        {
            report = new Report
            {
                unity = Application.unityVersion,
                gpu = SystemInfo.graphicsDeviceName,
                cpu = SystemInfo.processorType,
                api = SystemInfo.graphicsDeviceType.ToString(),
                repetitions = Repetitions,
                sampleSeconds = SampleSeconds
            };
            Application.logMessageReceived += OnLog;
            if (SceneManager.GetActiveScene().name != "FurnitureLightStudy")
            {
                report.failure = "Expected FurnitureLightStudy.";
                Finish();
                yield break;
            }

            measurementSettings = new NativeBenchmarkSettingsLease();
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            yield return null;
            report.width = Screen.width;
            report.height = Screen.height;
            report.vSync = QualitySettings.vSyncCount;
            report.targetFrameRate = Application.targetFrameRate;
            report.frameTimingEnabled = FrameTimingManager.IsFeatureEnabled();
            study = FindAnyObjectByType<FurnitureLightStudy2D>();
            flashlight = FindAnyObjectByType<Flashlight2D>();
            flashlight.GetComponent<PlayerMotor2D>().enabled = false;
            flashlight.enabled = false;
            keyboard = Keyboard.current;
            mouse = Mouse.current;
            keyboardWasEnabled = keyboard != null && keyboard.enabled;
            mouseWasEnabled = mouse != null && mouse.enabled;
            if (keyboardWasEnabled)
            {
                InputSystem.DisableDevice(keyboard);
            }
            if (mouseWasEnabled)
            {
                InputSystem.DisableDevice(mouse);
            }

            main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            render = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
            gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            report.mainRecorder = main.Valid;
            report.renderRecorder = render.Valid;
            report.gcRecorder = gc.Valid;
            report.drawRecorder = draws.Valid;
            report.batchRecorder = batches.Valid;
            var frameEnd = new WaitForEndOfFrame();

            // Warm both shaders before recording; include all objects and a ceiling-lit case.
            for (int variant = 0; variant < 3; variant++)
            {
                study.SetVariant(variant);
                study.SetCeiling(true);
                float until = Time.realtimeSinceStartup + 1;
                while (Time.realtimeSinceStartup < until)
                {
                    yield return frameEnd;
                }
            }

            for (int scenario = 0; scenario < 5; scenario++)
            {
                if (scenario == 2)
                {
                    CreateDenseFixture();
                }

                bool ceiling = scenario == 1 || scenario == 3;
                bool shadows = scenario != 4;
                study.SetCeiling(ceiling);
                study.SetShadows(shadows);
                for (int repetition = 0; repetition < Repetitions; repetition++)
                {
                    for (int order = 0; order < 3; order++)
                    {
                        int variant = Orders[repetition, order];
                        study.SetVariant(variant);
                        var segment = new Segment
                        {
                            scenario = scenario < 2 ? "authored-three" : "dense-sixty",
                            repetition = repetition,
                            variant = variant,
                            order = order,
                            ceiling = ceiling,
                            shadows = shadows,
                            activeFurniture = scenario < 2 ? 3 : copies.Count
                        };
                        report.segments.Add(segment);
                        File.WriteAllText(destination + ".progress", report.segments.Count + "/90");
                        if (repetition == 0 && order == 0)
                        {
                            ScreenCapture.CaptureScreenshot(destination + ".scenario" + scenario + ".png");
                        }
                        float ready = Time.realtimeSinceStartup + WarmupSeconds;
                        while (Time.realtimeSinceStartup < ready)
                        {
                            FrameTimingManager.CaptureFrameTimings();
                            yield return frameEnd;
                        }

                        float finishAt = Time.realtimeSinceStartup + SampleSeconds;
                        while (Time.realtimeSinceStartup < finishAt)
                        {
                            FrameTimingManager.CaptureFrameTimings();
                            yield return frameEnd;
                            uint count = FrameTimingManager.GetLatestTimings(1, timings);
                            FrameTiming timing = timings[0];
                            bool fresh = count > 0 && timing.frameStartTimestamp != previousTiming;
                            if (fresh)
                            {
                                previousTiming = timing.frameStartTimestamp;
                            }

                            segment.frames.Add(new FrameSample
                            {
                                frameMs = Time.unscaledDeltaTime * 1000,
                                mainMs = main.Valid ? main.LastValue / 1e6 : -1,
                                renderMs = render.Valid ? render.LastValue / 1e6 : -1,
                                gpuMs = fresh ? timing.gpuFrameTime : -1,
                                timingCpuMs = fresh ? timing.cpuFrameTime : -1,
                                presentWaitMs = fresh ? timing.cpuMainThreadPresentWaitTime : -1,
                                gcBytes = gc.Valid ? gc.LastValue : -1,
                                batches = batches.Valid ? batches.LastValue : -1,
                                draws = draws.Valid ? draws.LastValue : -1,
                                timingFresh = fresh
                            });
                        }
                    }
                }
            }

            Finish();
        }

        private void CreateDenseFixture()
        {
            study.SetVariant(0);
            var authored = study.Samples;
            originalSamples = authored;
            var allSamples = new List<FurnitureLightStudy2D.Sample>(authored);

            for (int i = 0; i < 60; i++)
            {
                var source = authored[i % authored.Length];
                Vector3 position = new Vector3((i % 10 - 4.5f) * .95f, (i / 10 - 2.5f) * .7f, 0);
                var obj = Instantiate(source.renderer.gameObject, position, Quaternion.identity);
                obj.name = "Runtime stress sample " + i;
                obj.transform.localScale = Vector3.one * .35f;
                foreach (var collider in obj.GetComponents<Collider2D>())
                {
                    collider.enabled = false;
                }

                var sprite = obj.GetComponent<SpriteRenderer>();
                allSamples.Add(new FurnitureLightStudy2D.Sample
                {
                    renderer = sprite,
                    responseIndex = source.responseIndex,
                    surfaceRect = source.surfaceRect
                });
                copies.Add(obj);
            }

            foreach (var sample in authored)
            {
                sample.renderer.gameObject.SetActive(false);
            }

            // Probe-only replacement: exercise the same controller loop with a larger runtime sample set.
            study.ReplaceRuntimeSamples(allSamples.ToArray());
            var camera = Camera.main;
            camera.GetComponent<CameraFollow2D>().enabled = false;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographicSize = 4.6f;
            flashlight.transform.position = new Vector3(0, -3.5f, 0);
            flashlight.GetComponent<Rigidbody2D>().position = flashlight.transform.position;
            flashlight.SetOptics(8, 110);
            flashlight.AimAt(new Vector2(0, 3));
            flashlight.SetPower(true);
            Physics2D.SyncTransforms();
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
            File.WriteAllText(destination, JsonUtility.ToJson(report));
            Application.Quit(string.IsNullOrEmpty(report.failure) ? 0 : 2);
        }

        private void OnDisable() => ReleaseMeasurement();
        private void OnDestroy() => ReleaseMeasurement();

        private void ReleaseMeasurement()
        {
            StopAllCoroutines();
            measurementSettings?.Dispose();
            measurementSettings = null;
            if (keyboardWasEnabled && keyboard != null && keyboard.added)
            {
                InputSystem.EnableDevice(keyboard);
            }

            if (mouseWasEnabled && mouse != null && mouse.added)
            {
                InputSystem.EnableDevice(mouse);
            }

            keyboardWasEnabled = false;
            mouseWasEnabled = false;
            if (study != null && originalSamples != null)
            {
                study.ReplaceRuntimeSamples(originalSamples);
                foreach (var sample in originalSamples)
                {
                    if (sample.renderer != null)
                    {
                        sample.renderer.gameObject.SetActive(true);
                    }
                }
            }

            originalSamples = null;
            foreach (GameObject copy in copies)
            {
                if (copy != null)
                {
                    Destroy(copy);
                }
            }

            copies.Clear();
            Application.logMessageReceived -= OnLog;
            main.Dispose();
            render.Dispose();
            gc.Dispose();
            batches.Dispose();
            draws.Dispose();
        }
    }
}
#endif
