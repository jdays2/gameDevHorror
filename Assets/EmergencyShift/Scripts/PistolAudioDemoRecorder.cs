#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Temporary Play-only comparison: real Game View plus Unity's rendered main audio output.</summary>
    public sealed class PistolAudioDemoRecorder : MonoBehaviour
    {
        public const int Fps = 30, FramesPerPass = 240;
        private string folder;
        private PlayerWeapon2D weapon;
        private PlayerAudio2D audioPlayer;
        private PlayerCasingEjector2D ejector;
        private Flashlight2D lamp;
        private GameObject enemyPrefab;
        private ShadowStalker2D enemy;
        private int phase = -1, shotIndex, previousCapture;
        private bool previousBackground, rendering;
        private BinaryWriter audioFile;
        private readonly int[] shotFrames = { 18, 50, 90, 101, 144, 154, 164 };
        [Serializable] private sealed class ShotSample { public int frame, phase; public string clip; public float pitch, gain; }
        [Serializable] private sealed class LandSample { public int frame, phase; public string clip; public Vector2 floor; }
        [Serializable] private sealed class FrameSample { public int frame, phase, casings, landed; public float light; }
        [Serializable] private sealed class Report
        {
            public int fps, frames, rate, channels, samples; public float peak;
            public string note; public List<ShotSample> shots; public List<LandSample> landings; public List<FrameSample> visuals;
        }
        private readonly List<ShotSample> shots = new List<ShotSample>();
        private readonly List<LandSample> landings = new List<LandSample>();
        private readonly List<FrameSample> visuals = new List<FrameSample>();
        private int samples;
        private float peak;
        public int FramesWritten { get; private set; }
        public bool Finished { get; private set; }
        public string Failure { get; private set; }

        public void Begin(string directory, GameObject targetPrefab)
        {
            folder = directory; enemyPrefab = targetPrefab; Directory.CreateDirectory(folder);
            weapon = UnityEngine.Object.FindFirstObjectByType<PlayerWeapon2D>();
            var player = weapon.transform; audioPlayer = player.GetComponent<PlayerAudio2D>(); ejector = player.GetComponent<PlayerCasingEjector2D>();
            foreach (var actor in UnityEngine.Object.FindObjectsByType<ShadowStalker2D>()) actor.gameObject.SetActive(false);
            foreach (var item in UnityEngine.Object.FindObjectsByType<WeaponLootPickup>()) item.gameObject.SetActive(false);
            foreach (var item in UnityEngine.Object.FindObjectsByType<FlashlightUpgradePickup>()) item.gameObject.SetActive(false);
            var corpses = GameObject.Find("Test corpses — Stalkers"); if (corpses != null) corpses.SetActive(false);
            typeof(PlayerLife2D).GetField("<Health>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player.GetComponent<PlayerLife2D>(), 2);
            player.GetComponent<PlayerMotor2D>().enabled = false; player.GetComponent<PlayerInteraction>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false; player.position = new Vector3(-1, 0, 0);
            weapon.enabled = true; if (!weapon.HasWeapon) weapon.Acquire(40); else weapon.TryAddAmmo(40);
            lamp = player.GetComponent<Flashlight2D>(); lamp.enabled = false;
            var camera = Camera.main; camera.GetComponent<CameraFollow2D>().enabled = false;
            camera.transform.position = new Vector3(.4f, 0, -10); camera.orthographicSize = 2.9f;
            audioPlayer.CombatAudioEnabled = true; audioPlayer.ImpactAudioEnabled = false;
            previousCapture = Time.captureFramerate; previousBackground = Application.runInBackground;
            Time.captureFramerate = Fps; Application.runInBackground = true;
            if (AudioSettings.speakerMode != AudioSpeakerMode.Stereo) throw new InvalidOperationException("Demo requires the existing stereo output.");
            if (!AudioRenderer.Start()) throw new InvalidOperationException("AudioRenderer is already recording.");
            rendering = true;
            audioFile = new BinaryWriter(File.Open(Path.Combine(folder, "unity-mix.f32"), FileMode.Create));
            ejector.CasingEmitted += ObserveCasing;
            StartCoroutine(Capture());
        }

        private void ObserveCasing(ShellCasing2D casing) { casing.FirstLanded += ObserveLanding; }
        private void ObserveLanding(ShellCasing2D casing)
        { landings.Add(new LandSample { frame = FramesWritten, phase = phase, clip = audioPlayer.LastCasingClip, floor = casing.FloorPosition }); }

        private void Update()
        {
            if (folder == null || Finished) return;
            int pass = FramesWritten / FramesPerPass;
            if (pass != phase)
            {
                if (enemy != null) Destroy(enemy.gameObject);
                foreach (var gib in UnityEngine.Object.FindObjectsByType<GibMotion2D>()) Destroy(gib.gameObject);
                foreach (var blood in UnityEngine.Object.FindObjectsByType<BloodPool2D>()) if (blood.name == "Blood pool") Destroy(blood.gameObject);
                ejector.ClearForPreview(); phase = pass; shotIndex = 0;
                ejector.CasingsEnabled = phase == 1;
                audioPlayer.SetPreviewSeed(174019); ejector.SetPreviewSeed(71401); UnityEngine.Random.InitState(41271);
                enemy = null;
            }
            int frame = FramesWritten % FramesPerPass;
            if (frame >= 80 && enemy == null)
            {
                enemy = Instantiate(enemyPrefab, new Vector3(1.7f, 0, 0), Quaternion.identity).GetComponent<ShadowStalker2D>();
                enemy.Configure(weapon.transform, lamp, 1 << 8); enemy.AttacksEnabled = false; enemy.enabled = false;
                enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                // Isolate weapon/casing A/B. Creature voice would randomize the comparison.
                enemy.GetComponent<StalkerAudio2D>().enabled = false;
            }
            if (enemy != null && enemy.Health == 0) enemy.enabled = true;
            lamp.AimAt(frame >= 80 && frame < 135 ? new Vector2(1.7f, 0) : new Vector2(-1, 5));
            lamp.SetPower((frame >= 75 && frame < 135) || frame >= 190);
            if (shotIndex < shotFrames.Length && frame >= shotFrames[shotIndex] && weapon.TryFire())
            {
                shots.Add(new ShotSample { frame = FramesWritten, phase = phase, clip = audioPlayer.LastShotClip, pitch = audioPlayer.LastShotPitch, gain = audioPlayer.LastShotGain });
                shotIndex++;
            }
        }

        private IEnumerator Capture()
        {
            while (FramesWritten < FramesPerPass * 2)
            {
                yield return new WaitForEndOfFrame();
                Texture2D image = null;
                try
                {
                    int count = AudioRenderer.GetSampleCountForCaptureFrame();
                    using (var buffer = new NativeArray<float>(count * 2, Allocator.Temp))
                    {
                        if (!AudioRenderer.Render(buffer)) throw new IOException("Unity main audio render failed.");
                        for (int i = 0; i < buffer.Length; i++) { audioFile.Write(buffer[i]); peak = Mathf.Max(peak, Mathf.Abs(buffer[i])); }
                        samples += count;
                    }
                    image = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(folder, "frame-" + FramesWritten.ToString("0000") + ".png"), image.EncodeToPNG());
                    var sample = new FrameSample { frame = FramesWritten, phase = phase };
                    foreach (var casing in UnityEngine.Object.FindObjectsByType<ShellCasing2D>()) { sample.casings++; if (casing.Landed) sample.landed++; }
                    foreach (var effect in UnityEngine.Object.FindObjectsByType<ShotEffect2D>()) sample.light = Mathf.Max(sample.light, effect.LightSource != null ? effect.LightSource.intensity : 0);
                    visuals.Add(sample); FramesWritten++;
                }
                catch (Exception ex) { Failure = ex.ToString(); break; }
                finally { if (image != null) Destroy(image); }
            }
            Finished = true; RestoreCapture();
            File.WriteAllText(Path.Combine(folder, "recording.json"), JsonUtility.ToJson(new Report { fps = Fps, frames = FramesWritten, channels = 2, rate = AudioSettings.outputSampleRate,
                samples = samples, peak = peak, shots = shots, landings = landings, visuals = visuals,
                note = "Real Unity Game View and AudioRenderer main mix, fixed capture timestep. Identical seed/shot frames. First pass no casings, second with casings. Play-only staged positions/frozen enemy; creature audio isolated. No offline audio overdub." }, true));
        }

        private void OnGUI()
        {
            if (folder == null || Finished) return;
            GUI.color = new Color(0, 0, 0, .78f); GUI.DrawTexture(new Rect(0, 0, Screen.width, 44), Texture2D.whiteTexture);
            GUI.color = Color.white; var style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, 0, Screen.width, 44), phase == 0 ? "БЕЗ ГИЛЬЗ · тот же пистолет и темп" : "С ГИЛЬЗАМИ · касание бетонного пола", style);
        }
        private void RestoreCapture()
        {
            if (rendering) { AudioRenderer.Stop(); rendering = false; }
            audioFile?.Dispose(); audioFile = null;
            Time.captureFramerate = previousCapture; Application.runInBackground = previousBackground;
        }
        private void OnDestroy()
        { if (folder == null) return; if (ejector != null) ejector.CasingEmitted -= ObserveCasing; RestoreCapture(); }
    }
}
#endif
