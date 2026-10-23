#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace EmergencyShift
{
    /// <summary>Temporary capture in the authored map, with its camera, lighting and audio intact.</summary>
    public sealed class WeaponFeedbackSceneProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Contact
        {
            public string species;
            public string weapon;
            public bool lethal;
            public int contacts;
            public int sounds;
            public int hpAfter;
        }

        [Serializable]
        private sealed class Report
        {
            public string scene;
            public float cameraSize;
            public Vector3 playerPosition;
            public int sampleRate;
            public float peak;
            public List<Contact> events = new List<Contact>();
        }

        private string folder;
        private LevelContext2D context;
        private PlayerWeapon2D weapon;
        private PlayerAudio2D playback;
        private Camera cameraView;
        private RenderTexture targetTexture;
        private Texture2D image;
        private BinaryWriter audio;
        private GameObject enemy;
        private bool recording;
        private int previousRate;
        private bool previousMute;
        private readonly Report report = new Report();

        public void Begin(string output)
        {
            folder = output;
            Directory.CreateDirectory(folder);
            context = FindAnyObjectByType<LevelContext2D>();
            weapon = context.Player.GetComponent<PlayerWeapon2D>();
            playback = weapon.GetComponent<PlayerAudio2D>();
            playback.SetPreviewSeed(42);
            context.Player.GetComponent<PlayerMotor2D>().enabled = false;
            context.Player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            foreach (var stalker in FindObjectsByType<ShadowStalker2D>())
            {
                stalker.AttacksEnabled = false;
            }
            cameraView = Camera.main;
            report.scene = gameObject.scene.name;
            report.cameraSize = cameraView.orthographicSize;
            report.playerPosition = context.Player.transform.position;
            report.sampleRate = AudioSettings.outputSampleRate;
            targetTexture = new RenderTexture(960, 540, 24);
            image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            if (!AudioRenderer.Start())
            {
                throw new InvalidOperationException("Audio capture is already active.");
            }
            previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            previousMute = EditorUtility.audioMasterMute;
            EditorUtility.audioMasterMute = false;
            recording = true;
            audio = new BinaryWriter(File.Create(Path.Combine(folder, "mix.f32")));
            StartCoroutine(Capture());
        }

        private IEnumerator Capture()
        {
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            var shotgun = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PumpShotgun.asset");
            weapon.Acquire(30, pistol);
            weapon.Acquire(30, shotgun);
            int contactsBefore = 0;
            int soundsBefore = 0;
            Contact current = null;
            for (int frame = 0; frame < 260; frame++)
            {
                if (frame % 60 == 0 && frame < 240)
                {
                    int section = frame / 60;
                    if (enemy != null)
                    {
                        Destroy(enemy);
                    }
                    current = new Contact { species = section < 2 ? "ShadowStalker" : "Sleeper", weapon = section < 2 ? "PistolDefinition" : "PumpShotgun", lethal = section % 2 == 1 };
                    weapon.TryEquip(section < 2 ? pistol.SelectionSlot : shotgun.SelectionSlot);
                    Vector3 position = context.Player.transform.position + Vector3.right * 2.6f;
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + current.species + ".prefab");
                    enemy = context.CreateInstance(prefab, position, Quaternion.identity);
                    enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    enemy.GetComponent<EnemyHealth2D>().Initialize(current.lethal ? 1 : 100);
                    var stalker = enemy.GetComponent<ShadowStalker2D>();
                    if (stalker != null)
                    {
                        stalker.AttacksEnabled = false;
                    }
                    context.Player.GetComponent<PlayerAim2D>().SetScriptedTarget(position);
                }
                if (frame % 60 == 15 && frame < 240)
                {
                    contactsBefore = playback.ResolvedImpactCount;
                    soundsBefore = playback.ImpactAudioCount;
                    weapon.TryFire();
                }
                if (frame % 60 == 45 && frame < 240)
                {
                    current.contacts = playback.ResolvedImpactCount - contactsBefore;
                    current.sounds = playback.ImpactAudioCount - soundsBefore;
                    current.hpAfter = enemy.GetComponent<EnemyHealth2D>().Current;
                    report.events.Add(current);
                }
                yield return null;
                Render(Path.Combine(folder, frame.ToString("D4") + ".png"));
                if (frame == 22)
                {
                    var visible = new List<SpriteRenderer>();
                    foreach (var cloud in FindObjectsByType<MuzzleSmoke2D>())
                    {
                        foreach (var renderer in cloud.GetComponentsInChildren<SpriteRenderer>())
                        {
                            if (renderer.enabled)
                            {
                                visible.Add(renderer);
                                renderer.enabled = false;
                            }
                        }
                    }
                    Render(Path.Combine(folder, "without-smoke.png"));
                    foreach (var renderer in visible)
                    {
                        renderer.enabled = true;
                    }
                }
                int samples = AudioRenderer.GetSampleCountForCaptureFrame();
                using (var buffer = new NativeArray<float>(samples * 2, Allocator.Temp))
                {
                    if (!AudioRenderer.Render(buffer))
                    {
                        StopCapture();
                        throw new IOException("AudioRenderer failed.");
                    }
                    foreach (float value in buffer)
                    {
                        audio.Write(value);
                        report.peak = Mathf.Max(report.peak, Mathf.Abs(value));
                    }
                }
            }
            StopCapture();
            File.WriteAllText(Path.Combine(folder, "report.json"), JsonUtility.ToJson(report, true));
        }

        private void Render(string path)
        {
            RenderTexture previous = RenderTexture.active;
            var cameraTarget = cameraView.targetTexture;
            cameraView.targetTexture = targetTexture;
            cameraView.Render();
            RenderTexture.active = targetTexture;
            image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            cameraView.targetTexture = cameraTarget;
            RenderTexture.active = previous;
        }

        private void StopCapture()
        {
            if (!recording)
            {
                return;
            }
            recording = false;
            AudioRenderer.Stop();
            audio?.Dispose();
            Time.captureFramerate = previousRate;
            EditorUtility.audioMasterMute = previousMute;
        }

        private void OnDestroy()
        {
            StopCapture();
            if (targetTexture != null)
            {
                targetTexture.Release();
                Destroy(targetTexture);
            }
            if (image != null)
            {
                Destroy(image);
            }
        }
    }
}
#endif
