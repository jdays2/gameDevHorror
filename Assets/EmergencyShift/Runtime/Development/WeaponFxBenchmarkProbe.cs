#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace EmergencyShift
{
    public sealed class WeaponFxBenchmarkProbe : MonoBehaviour
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private GameObject baselineEffectPrefab;
        private bool baseline;

        public void ConfigureBaseline(GameObject effect) => baselineEffectPrefab = effect;

        public void Configure(GameObject effect, GameObject bullet, WeaponDefinition weapon)
        {
            effectPrefab = effect;
            bulletPrefab = bullet;
            definition = weapon;
        }

        private void Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            baseline = Array.IndexOf(arguments, "--fx-baseline") >= 0;
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == "--fx-benchmark")
                {
                    Application.runInBackground = true;
                    Begin(arguments[index + 1]);
                    return;
                }
            }
        }
        [Serializable]
        private sealed class Result
        {
            public string device;
            public bool baselineWithoutSmoke;
            public bool frameTimingEnabled;
            public int frames;
            public int volleys;
            public int pelletsPerVolley = 8;
            public bool gpuTimingAvailable;
            public List<double> cpuMs = new List<double>();
            public List<double> renderMs = new List<double>();
            public List<double> gpuMs = new List<double>();
            public List<long> gcBytes = new List<long>();
            public List<long> draws = new List<long>();
        }

        public void Begin(string output) => StartCoroutine(Measure(output));

        private IEnumerator Measure(string output)
        {
            var context = FindFirstObjectByType<LevelContext2D>();
            foreach (var receiver in FindObjectsByType<Damageable2D>())
            {
                if (receiver.GetComponent<PlayerLife2D>() == null)
                {
                    receiver.gameObject.SetActive(false);
                }
            }
            var player = context.Player;
            var camera = Camera.main;
            foreach (var behaviour in camera.GetComponents<MonoBehaviour>())
            {
                behaviour.enabled = false;
            }
            camera.transform.position = new Vector3(102, 100, -10);
            camera.orthographicSize = 2.8f;
            player.GetComponent<PlayerMotor2D>().enabled = false;
            player.GetComponent<Rigidbody2D>().position = new Vector2(100, 100);
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            var wall = new GameObject("Transient FX benchmark wall", typeof(BoxCollider2D));
            wall.layer = 8;
            wall.transform.position = new Vector3(104, 100, 0);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(.1f, 3);
#if UNITY_EDITOR
            if (effectPrefab == null)
            {
                Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab"),
                    AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PumpShotgun.asset"));
            }
#endif
            var effect = baseline && baselineEffectPrefab != null ? baselineEffectPrefab : effectPrefab;
            if (baseline && baselineEffectPrefab == null)
            {
                throw new InvalidOperationException("The benchmark requires its original muzzle prefab for a baseline run.");
            }
            var bullet = bulletPrefab;
            var weapon = definition;
            var timing = new FrameTiming[1];
            var result = new Result { device = SystemInfo.graphicsDeviceName, baselineWithoutSmoke = baseline,
                frameTimingEnabled = FrameTimingManager.IsFeatureEnabled() };
            var target = new RenderTexture(960, 540, 24);
            var oldTarget = camera.targetTexture;
            camera.targetTexture = target;
            int oldRate = Time.captureFramerate;
            Time.captureFramerate = 60;
            using (var cpu = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread"))
            using (var render = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread"))
            using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            {
                try
                {
                    for (int frame = 0; frame < 480; frame++)
                    {
                        if (frame % 6 == 0)
                        {
                            var flash = context.CreateInstance(effect, new Vector3(101, 100, -.1f), Quaternion.identity).GetComponent<ShotEffect2D>();
                            flash.Play(weapon.Visuals, false, Vector2.right);
                            for (int pellet = 0; pellet < 8; pellet++)
                            {
                                var flight = context.CreateInstance(bullet, new Vector3(101, 100, -.1f), Quaternion.identity).GetComponent<BulletProjectile2D>();
                                var direction = Quaternion.Euler(0, 0, (pellet - 3.5f) * 2) * Vector2.right;
                                flight.Launch(player.transform, direction, weapon.Visuals, effect.GetComponent<ShotEffect2D>(), 1 << 8, 1, 8, frame + 1);
                            }
                            result.volleys++;
                        }
                        FrameTimingManager.CaptureFrameTimings();
                        camera.Render();
                        yield return null;
                        if (frame >= 120)
                        {
                            result.frames++;
                            result.cpuMs.Add(cpu.LastValue / 1e6);
                            result.renderMs.Add(render.LastValue / 1e6);
                            result.gcBytes.Add(gc.LastValue);
                            result.draws.Add(draws.LastValue);
                            if (FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].gpuFrameTime > 0)
                            {
                                result.gpuTimingAvailable = true;
                                result.gpuMs.Add(timing[0].gpuFrameTime);
                            }
                        }
                    }
                }
                finally
                {
                    Time.captureFramerate = oldRate;
                    camera.targetTexture = oldTarget;
                    target.Release();
                    Destroy(target);
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
#if !UNITY_EDITOR
            Application.Quit();
#endif
        }
    }
}
#endif
