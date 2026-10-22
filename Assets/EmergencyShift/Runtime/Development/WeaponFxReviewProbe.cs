#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Transient real weapon/mix capture. Never saved in a scene or included in a release.</summary>
    public sealed class WeaponFxReviewProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class ContactEvent
        {
            public string weapon;
            public int material;
            public int resolvedContacts;
            public int audibleImpacts;
            public int fleshReactions;
        }

        [Serializable]
        private sealed class ContactReport
        {
            public List<ContactEvent> events = new List<ContactEvent>();
        }

        private LevelContext2D context;
        private PlayerWeapon2D weapon;
        private GameObject target;
        private BinaryWriter audio;
        private string folder;
        private int previousCaptureRate;
        private bool recording;
        private int shots;
        private float peak;
        private Camera captureCamera;
        private RenderTexture captureTarget;
        private Texture2D captureImage;
        private readonly ContactReport contacts = new ContactReport();
        private int contactsBefore;
        private int audibleBefore;
        private int fleshBefore;
        public bool Finished { get; private set; }

        public void Begin(string output)
        {
            folder = output;
            Directory.CreateDirectory(folder);
            context = FindFirstObjectByType<LevelContext2D>();
            foreach (var enemy in FindObjectsByType<Damageable2D>())
            {
                if (enemy.GetComponent<PlayerLife2D>() == null)
                {
                    enemy.gameObject.SetActive(false);
                }
            }
            foreach (var director in FindObjectsByType<LevelAudioDirector2D>())
            {
                director.enabled = false;
            }
            var player = context.Player;
            player.GetComponent<PlayerMotor2D>().enabled = false;
            player.GetComponent<Rigidbody2D>().position = new Vector2(100, 100);
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            player.GetComponent<Flashlight2D>().SetPower(true);
            player.GetComponent<PlayerAim2D>().SetScriptedTarget(new Vector2(103, 100));
            weapon = player.GetComponent<PlayerWeapon2D>();
            weapon.GetComponent<PlayerAudio2D>().SetPreviewSeed(42);
            var camera = Camera.main;
            captureCamera = camera;
            captureTarget = new RenderTexture(960, 540, 24);
            captureImage = new Texture2D(960, 540, TextureFormat.RGB24, false);
            foreach (var behaviour in camera.GetComponents<MonoBehaviour>())
            {
                behaviour.enabled = false;
            }
            camera.transform.position = new Vector3(101.1f, 100, -10);
            camera.orthographicSize = 2.8f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab");
            var floor = new GameObject("Transient capture floor", typeof(SpriteRenderer));
            var renderer = floor.GetComponent<SpriteRenderer>();
            renderer.sprite = prefab.GetComponentInChildren<SpriteRenderer>().sprite;
            renderer.color = new Color(.055f, .065f, .075f, 1);
            renderer.sortingOrder = -20;
            floor.transform.position = new Vector3(101, 100, 0);
            floor.transform.localScale = new Vector3(12, 7, 1);
            previousCaptureRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            if (!AudioRenderer.Start())
            {
                throw new InvalidOperationException("Audio capture is already active.");
            }
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
            for (int frame = 0; frame < 390; frame++)
            {
                if (frame % 60 == 0 && frame < 360)
                {
                    int section = frame / 60;
                    weapon.TryEquip(section < 3 ? pistol.SelectionSlot : shotgun.SelectionSlot);
                    StageTarget(section % 3);
                }
                if (frame % 60 == 15 && frame < 360)
                {
                    var playback = weapon.GetComponent<PlayerAudio2D>();
                    contactsBefore = playback.ResolvedImpactCount;
                    audibleBefore = playback.ImpactAudioCount;
                    var voice = target.GetComponent<StalkerAudio2D>();
                    fleshBefore = voice != null ? voice.PlaybackCount(StalkerAudioSet.Cue.Hit) : 0;
                    if (weapon.TryFire())
                    {
                        shots++;
                    }
                }
                if (frame % 60 == 50 && frame < 360)
                {
                    var playback = weapon.GetComponent<PlayerAudio2D>();
                    var voice = target.GetComponent<StalkerAudio2D>();
                    contacts.events.Add(new ContactEvent
                    {
                        weapon = weapon.Definition.ContentId,
                        material = frame / 60 % 3,
                        resolvedContacts = playback.ResolvedImpactCount - contactsBefore,
                        audibleImpacts = playback.ImpactAudioCount - audibleBefore,
                        fleshReactions = voice != null ? voice.PlaybackCount(StalkerAudioSet.Cue.Hit) - fleshBefore : 0
                    });
                }
                yield return null;
                var previousTarget = captureCamera.targetTexture;
                var previousActive = RenderTexture.active;
                captureCamera.targetTexture = captureTarget;
                captureCamera.Render();
                RenderTexture.active = captureTarget;
                captureImage.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                captureImage.Apply();
                File.WriteAllBytes(Path.Combine(folder, frame.ToString("D4") + ".png"), captureImage.EncodeToPNG());
                captureCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                int samples = AudioRenderer.GetSampleCountForCaptureFrame();
                using (var buffer = new NativeArray<float>(samples * 2, Allocator.Temp))
                {
                    if (!AudioRenderer.Render(buffer))
                    {
                        throw new IOException("AudioRenderer failed.");
                    }
                    foreach (float value in buffer)
                    {
                        audio.Write(value);
                        peak = Mathf.Max(peak, Mathf.Abs(value));
                    }
                }
            }
            StopCapture();
            Finished = true;
            File.WriteAllText(Path.Combine(folder, "contacts.json"), JsonUtility.ToJson(contacts, true));
            File.WriteAllText(Path.Combine(folder, "capture.json"),
                "{\"shots\":" + shots + ",\"frames\":390,\"fps\":30,\"sampleRate\":" + AudioSettings.outputSampleRate +
                ",\"peak\":" + peak.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
        }

        private void StageTarget(int material)
        {
            if (target != null)
            {
                Destroy(target);
            }
            if (material == 2)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
                target = context.CreateInstance(prefab, new Vector3(103, 100, 0), Quaternion.identity);
                target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                target.GetComponent<EnemyHealth2D>().Initialize(100);
                target.GetComponent<ShadowStalker2D>().AttacksEnabled = false;
                return;
            }
            target = new GameObject("Capture impact surface", typeof(BoxCollider2D), typeof(ImpactSurface2D));
            target.layer = 8;
            target.transform.position = new Vector3(103, 100, 0);
            target.GetComponent<BoxCollider2D>().size = new Vector2(.2f, 2);
            JsonUtility.FromJsonOverwrite("{\"material\":" + material + "}", target.GetComponent<ImpactSurface2D>());
            Physics2D.SyncTransforms();
        }

        private void StopCapture()
        {
            if (!recording)
            {
                return;
            }
            audio?.Dispose();
            audio = null;
            AudioRenderer.Stop();
            Time.captureFramerate = previousCaptureRate;
            recording = false;
            if (captureTarget != null)
            {
                captureTarget.Release();
                Destroy(captureTarget);
                Destroy(captureImage);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            StopCapture();
        }
    }
}
#endif
