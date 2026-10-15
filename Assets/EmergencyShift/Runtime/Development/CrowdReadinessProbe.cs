#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Temporary authored-prefab stress fixture. Records actual CPU markers, not a frame-rate guarantee.</summary>
    public sealed class CrowdReadinessProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Sample
        {
            public double frameMs;
            public double tickMs;
            public double pathMs;
            public double separationMs;
            public long gcBytes;
        }

        [Serializable]
        private sealed class Result
        {
            public int actors;
            public int automaticBindings;
            public int movedActors;
            public int pathBuilds;
            public int peakPathBuilds;
            public int peakFragments;
            public int wallPenetrations;
            public int settledParts;
            public int pools;
            public int activeAI;
            public int activeAnimators;
            public int activeAudio;
            public int updatingPools;
            public double coldSpawnMs;
            public double killDispatchMs;
            public float initialAverageDistance;
            public float finalAverageDistance;
            public float minimumNeighbourDistance;
            public bool tickRecorder;
            public bool pathRecorder;
            public bool separationRecorder;
            public bool gcRecorder;
            public string note;
            public List<Sample> samples;
        }

        private readonly List<ShadowStalker2D> actors = new List<ShadowStalker2D>();
        private readonly List<Vector2> origins = new List<Vector2>();
        private readonly List<Sample> samples = new List<Sample>();
        private ProfilerRecorder ticks;
        private ProfilerRecorder paths;
        private ProfilerRecorder separation;
        private ProfilerRecorder gc;
        private string folder;
        private PlayerLife2D player;
        private BoxCollider2D barrier;
        private Result result;
        private int initialBuilds;
        private bool previousBackground;
        public bool Finished { get; private set; }
        public string Failure { get; private set; }
        public int Frames => samples.Count;

        public void Begin(string output, int count = 20)
        {
            folder = output;
            Directory.CreateDirectory(folder);
            result = new Result();
            previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            foreach (var e in FindObjectsByType<ShadowStalker2D>())
            {
                e.gameObject.SetActive(false);
            }

            foreach (var g in FindObjectsByType<GibMotion2D>())
            {
                g.gameObject.SetActive(false);
            }

            foreach (var p in FindObjectsByType<BloodPool2D>())
            {
                p.gameObject.SetActive(false);
            }

            player = FindAnyObjectByType<PlayerLife2D>();
            typeof(PlayerLife2D).GetField("<Health>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, 1000);
            player.GetComponent<PlayerMotor2D>().enabled = false;
            player.GetComponent<PlayerInteraction>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            player.transform.position = new Vector3(100, 100, 0);
            var light = player.GetComponent<Flashlight2D>();
            light.enabled = false;
            light.SetPower(false);
            var camera = Camera.main;
            camera.GetComponent<CameraFollow2D>().enabled = false;
            camera.transform.position = new Vector3(98, 100, -10);
            camera.orthographicSize = 6;
            var wall = new GameObject("Temporary crowd route wall", typeof(BoxCollider2D));
            wall.layer = 8;
            wall.transform.position = new Vector3(98, 100, 0);
            barrier = wall.GetComponent<BoxCollider2D>();
            barrier.size = new Vector2(.25f, 5);
            var floor = new GameObject("Temporary crowd floor", typeof(SpriteRenderer));
            floor.transform.position = new Vector3(98, 100, .1f);
            floor.transform.localScale = new Vector3(18, 14, 1);
            var image = floor.GetComponent<SpriteRenderer>();
            image.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergencyShift/Art/Shared/Shapes/Rectangle.asset");
            image.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/FlatLit.mat");
            image.color = new Color(.2f, .22f, .23f);
            image.sortingOrder = -10;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                var position = new Vector2(93 + (i % 4) * .8f, 97 + (i / 4) * 1.05f);
                var actor = Instantiate(prefab, position, Quaternion.identity).GetComponent<ShadowStalker2D>();
                actor.AttacksEnabled = false;
                actor.ApplyBulletHit(1, position, Vector2.right);
                actors.Add(actor);
                origins.Add(position);
                if (actor.Player == player.transform && actor.LightSource == light)
                {
                    result.automaticBindings++;
                }

                result.initialAverageDistance += Vector2.Distance(position, player.transform.position) / count;
            }

            watch.Stop();
            result.coldSpawnMs = watch.Elapsed.TotalMilliseconds;
            result.actors = count;
            initialBuilds = NavigationWorkBudget2D.TotalBuilds;
            Physics2D.SyncTransforms();
            var options = ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately;
            ticks = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "EmergencyShift.Stalker.Tick", 1, options);
            paths = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "EmergencyShift.Navigation.Build", 1, options);
            separation = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "EmergencyShift.Crowd.Separation", 1, options);
            gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1, options);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            float start = Time.time;
            while (Time.time - start < 12)
            {
                yield return new WaitForEndOfFrame();
                if (Time.time - start < 2)
                {
                    continue;
                }

                samples.Add(new Sample { frameMs = Time.unscaledDeltaTime * 1000, tickMs = ticks.Valid ? ticks.LastValue / 1e6 : -1, pathMs = paths.Valid ? paths.LastValue / 1e6 : -1, separationMs = separation.Valid ? separation.LastValue / 1e6 : -1, gcBytes = gc.Valid ? gc.LastValue : -1 });
                foreach (var actor in actors)
                {
                    if (actor.GetComponent<Collider2D>().Distance(barrier).distance < -.04f)
                    {
                        result.wallPenetrations++;
                    }
                }
            }

            result.tickRecorder = ticks.Valid;
            result.pathRecorder = paths.Valid;
            result.separationRecorder = separation.Valid;
            result.gcRecorder = gc.Valid;
            ticks.Dispose();
            paths.Dispose();
            separation.Dispose();
            gc.Dispose();
            result.minimumNeighbourDistance = float.MaxValue;
            for (int i = 0; i < actors.Count; i++)
            {
                if (Vector2.Distance(origins[i], actors[i].transform.position) > .5f)
                {
                    result.movedActors++;
                }

                result.finalAverageDistance += Vector2.Distance(actors[i].transform.position, player.transform.position) / actors.Count;
                for (int j = i + 1; j < actors.Count; j++)
                {
                    result.minimumNeighbourDistance = Mathf.Min(result.minimumNeighbourDistance, Vector2.Distance(actors[i].transform.position, actors[j].transform.position));
                }
            }

            result.pathBuilds = NavigationWorkBudget2D.TotalBuilds - initialBuilds;
            result.peakPathBuilds = NavigationWorkBudget2D.PeakBuilds;
            foreach (var light in FindObjectsByType<Light2D>())
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    light.intensity = .6f;
                    break;
                }
            }

            yield return new WaitForEndOfFrame();
            SaveImage("alive.png");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var actor in actors)
            {
                actor.Kill();
            }

            watch.Stop();
            result.killDispatchMs = watch.Elapsed.TotalMilliseconds;
            yield return new WaitForSeconds(4);
            result.peakFragments = GoreSpawnBudget2D.PeakFragments;
            foreach (var actor in actors)
            {
                if (actor.isActiveAndEnabled)
                {
                    result.activeAI++;
                }

                if (actor.GetComponent<StalkerSpriteAnimator>().isActiveAndEnabled)
                {
                    result.activeAnimators++;
                }

                if (actor.GetComponent<StalkerAudio2D>().isActiveAndEnabled)
                {
                    result.activeAudio++;
                }
            }

            foreach (var g in FindObjectsByType<GibMotion2D>())
            {
                if (g.gameObject.activeInHierarchy && g.Settled)
                {
                    result.settledParts++;
                }
            }

            foreach (var p in FindObjectsByType<BloodPool2D>())
            {
                if (p.gameObject.activeInHierarchy)
                {
                    result.pools++;
                    if (p.enabled)
                    {
                        result.updatingPools++;
                    }
                }
            }

            yield return new WaitForEndOfFrame();
            SaveImage("remains.png");
            result.samples = samples;
            result.note = "Editor Play, 20 real Stalker prefabs with default animations/audio/shadows. Bullet provocation, temporary wall detour, attacks disabled. Movement warmup2s, measured10s. Diagnostic global reveal only after timing, not production lighting or GPU benchmark. Whole-editor GC includes tools/editor overhead. All temporary, no scene save.";
            File.WriteAllText(Path.Combine(folder, "report.json"), JsonUtility.ToJson(result, true));
            Finished = true;
            Application.runInBackground = previousBackground;
        }

        private void SaveImage(string name)
        {
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, name), image.EncodeToPNG());
            Destroy(image);
        }

        private void OnDestroy()
        {
            ticks.Dispose();
            paths.Dispose();
            separation.Dispose();
            gc.Dispose();
            Application.runInBackground = previousBackground;
        }
    }
}
#endif
