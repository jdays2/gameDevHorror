#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Temporary Editor Play probe/real main-mix recorder. Never saved and excluded from builds.</summary>
    public sealed class AdaptiveAudioProbe : MonoBehaviour
    {
        [Serializable]
        public sealed class Check
        {
            public string name;
            public bool passed;
            public float time;
        }

        [Serializable]
        public sealed class Report
        {
            public string failure;
            public string mode;
            public bool finished;
            public int frames;
            public int sampleRate;
            public float peak;
            public Check[] checks;
            public string[] timeline;
        }

        private readonly List<Check> checks = new List<Check>();
        private readonly List<string> timeline = new List<string>();
        private LevelAudioDirector2D director;
        private LevelContext2D context;
        private PlayerLife2D player;
        private PlayerWeapon2D weapon;
        private Flashlight2D flashlight;
        private PlayerFootsteps2D feet;
        private Rigidbody2D playerBody;
        private ShadowStalker2D actor;
        private float previousTimeScale;
        private bool ownsTimeScale;
        private AudioMoodProfile originalProfile;
        private AudioMoodProfile testProfile;
        private BinaryWriter writer;
        private string folder;
        private string mode;
        private bool capturing;
        private bool previousBackground;
        private int previousCapture;
        private int shots;
        public bool Finished { get; private set; }
        public string Failure { get; private set; }
        public int Frames { get; private set; }
        public float Peak { get; private set; }

        private static void Set(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null)
            {
                throw new InvalidOperationException(target.GetType().Name + "." + field);
            }

            info.SetValue(target, value);
        }

        private static T Get<T>(object target, string field)
        {
            return (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        }

        private void CheckThat(string name, bool passed)
        {
            checks.Add(new Check { name = name, passed = passed, time = Time.time });
            if (!passed)
            {
                throw new InvalidOperationException("Failed: " + name);
            }
        }

        private void Prepare()
        {
            if (!ownsTimeScale)
            {
                previousTimeScale = Time.timeScale;
                ownsTimeScale = true;
            }

            context = GetComponentInScene<LevelContext2D>();
            foreach (var enemy in FindObjectsByType<ShadowStalker2D>())
            {
                if (enemy.gameObject.scene == gameObject.scene)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            director = GetComponentInScene<LevelAudioDirector2D>();
            player = context.Player;
            weapon = player.GetComponent<PlayerWeapon2D>();
            flashlight = player.GetComponent<Flashlight2D>();
            feet = player.GetComponent<PlayerFootsteps2D>();
            player.GetComponent<PlayerMotor2D>().enabled = false;
            playerBody = player.GetComponent<Rigidbody2D>();
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = new Vector2(100, 100);
            flashlight.SetPower(false);
            weapon.Acquire(50);
            originalProfile = director.Profile;
        }

        private T GetComponentInScene<T>() where T : Component
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                T candidate = root.GetComponentInChildren<T>(true);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Audio probe requires " + typeof(T).Name + " in its own scene.");
        }

        private GameObject CreateProbeObject(string name)
        {
            var result = new GameObject(name);
            SceneManager.MoveGameObjectToScene(result, gameObject.scene);
            return result;
        }

        private ShadowStalker2D Spawn(Vector2 position, bool audible, bool attacks = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            var obj = context.CreateInstance(prefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            var result = obj.GetComponent<ShadowStalker2D>();
            result.Configure(player.transform, flashlight, 1 << 8);
            result.AttacksEnabled = attacks;
            // Freeze staging movement while keeping the real collider available to projectiles.
            obj.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            obj.GetComponent<StalkerAudio2D>().enabled = audible;
            return result;
        }

        public void BeginTests(string path)
        {
            folder = path;
            Directory.CreateDirectory(folder);
            mode = "tests";
            Prepare();
            testProfile = Instantiate(originalProfile);
            var tuning = new SerializedObject(testProfile);
            tuning.FindProperty("threatHoldSeconds").floatValue = .5f;
            tuning.FindProperty("combatHoldSeconds").floatValue = .35f;
            tuning.FindProperty("perceptionMemorySeconds").floatValue = .2f;
            tuning.FindProperty("riseSeconds").floatValue = .1f;
            tuning.FindProperty("fallSeconds").floatValue = .1f;
            tuning.ApplyModifiedPropertiesWithoutUndo();
            testProfile.Validate();
            Set(director, "profile", testProfile);
            director.enabled = false;
            director.enabled = true;
            StartCoroutine(Guard(Tests()));
        }

        private IEnumerator Tests()
        {
            yield return new WaitForSeconds(.4f);
            var bed = Get<AudioSource>(director, "explorationSource");
            var threat = Get<AudioSource>(director, "threatSource");
            var combat = Get<AudioSource>(director, "combatSource");
            CheckThat("Three bounded sources playing synchronous9s PCM loops", bed.isPlaying && threat.isPlaying && combat.isPlaying && bed.loop && threat.loop && combat.loop && bed.clip.samples == threat.clip.samples && bed.clip.samples == combat.clip.samples && Mathf.Abs(bed.timeSamples - threat.timeSamples) < 1024);
            actor = Spawn(new Vector2(103, 100), false);
            var wall = CreateProbeObject("Probe high wall");
            wall.layer = 8;
            wall.transform.position = new Vector3(101.5f, 100, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.3f, 4);
            Physics2D.SyncTransforms();
            actor.ApplyBulletHit(1, actor.transform.position, Vector2.right);
            yield return new WaitForSeconds(.35f);
            CheckThat("Silent unseen aggro behind wall does not reveal enemy", actor.HasAggro && director.Mood == AudioMood.Explore && director.PerceivedEnemies == 0);
            flashlight.AimAt((Vector2)player.transform.position + Vector2.up * 10);
            weapon.TryFire();
            CheckThat("Real shot immediately ducks atmosphere", director.IsDucking);
            yield return new WaitForSeconds(.3f);
            CheckThat("Shooting an empty area does not start combat", director.Mood == AudioMood.Explore);
            var other = SceneManager.CreateScene("Audio probe other scene");
            var foreign = Spawn(new Vector2(100, 103), false);
            SceneManager.MoveGameObjectToScene(foreign.gameObject, other);
            flashlight.SetPower(true);
            flashlight.AimAt(foreign.transform.position);
            yield return new WaitForSeconds(.3f);
            CheckThat("Visible source in another scene is ignored", director.PerceivedEnemies == 0);
            yield return SceneManager.UnloadSceneAsync(other);
            yield return new WaitForSeconds(1.7f);
            flashlight.SetPower(false);
            var lampObject = CreateProbeObject("Probe lamp");
            lampObject.transform.position = new Vector3(103, 100.2f, 0);
            var light = lampObject.AddComponent<Light2D>();
            var lamp = lampObject.AddComponent<LocalLamp2D>();
            context.InjectScoped(lamp);
            lamp.Configure(light, null, Color.white, false);
            lamp.SetPower(true);
            yield return new WaitForSeconds(.3f);
            CheckThat("Lamp behind the player's wall does not reveal creature", director.PerceivedEnemies == 0);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.4f);
            CheckThat("Local lamp reveals creature without fear-light activation", director.Mood == AudioMood.Threat && director.PerceivedEnemies == 1 && !actor.Exposed);
            lamp.SetPower(false);
            yield return new WaitForSeconds(.9f);
            CheckThat("Darkness and expired memory remove lamp visibility", director.Mood == AudioMood.Explore);
            flashlight.SetPower(true);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            flashlight.AimAt(actor.transform.position);
            yield return new WaitForSeconds(.4f);
            CheckThat("Flashlight discovery enters Threat", director.Mood == AudioMood.Threat && director.PerceivedEnemies == 1 && director.LayerGains.y > 0);
            weapon.TryFire();
            yield return new WaitForSeconds(.18f);
            CheckThat("Known enemy is killed by the actual projectile", !actor.IsAlive);
            CheckThat("Shot at a known enemy enters Combat even when projectile kills it", director.Mood == AudioMood.Combat);
            if (actor.IsAlive)
            {
                actor.Kill();
            }

            flashlight.SetPower(false);
            yield return new WaitForSeconds(1.4f);
            CheckThat("Death and expired perception return to Explore", director.Mood == AudioMood.Explore);
            actor = Spawn((Vector2)player.transform.position + Vector2.right * 2.1f, false);
            flashlight.SetPower(true);
            flashlight.AimAt(actor.transform.position);
            yield return new WaitForSeconds(.4f);
            CheckThat("One perceived close pursuer starts Combat without a shot or attack", actor.HasAggro && director.EngagedEnemies == 1 && director.Mood == AudioMood.Combat);
            actor.Kill();
            flashlight.SetPower(false);
            yield return new WaitForSeconds(1.4f);
            actor = Spawn((Vector2)player.transform.position + Vector2.right * 3, false);
            var crowdMate = Spawn((Vector2)player.transform.position + new Vector2(3, .5f), false);
            flashlight.SetPower(true);
            flashlight.AimAt(actor.transform.position);
            yield return new WaitForSeconds(.5f);
            CheckThat("Two perceived pursuers start Combat outside close radius without a shot", director.EngagedEnemies == 2 && director.Mood == AudioMood.Combat);
            actor.Kill();
            crowdMate.Kill();
            flashlight.SetPower(false);
            yield return new WaitForSeconds(1.4f);
            actor = Spawn((Vector2)player.transform.position + Vector2.right * 1.05f, true, true);
            yield return new WaitForSeconds(.3f);
            CheckThat("Actually heard attack cue starts Combat without flashlight", director.Mood == AudioMood.Combat && director.PerceivedEnemies > 0 && actor.GetComponent<StalkerAudio2D>().PlaybackCount(StalkerAudioSet.Cue.Windup) > 0);
            actor.AttacksEnabled = false;
            actor.Kill();
            yield return new WaitForSeconds(1f);
            CheckThat("Nonlethal player damage accepted", player.TakeDamage(1));
            CheckThat("Damage immediately ducks background", director.IsDucking);
            yield return new WaitForSeconds(.2f);
            CheckThat("Damage escalates combat below full health", player.Health == player.MaximumHealth - 1 && director.Mood == AudioMood.Combat);
            var utility = player.GetComponent<PlayerUtilityAudio2D>();
            int switches = utility.SwitchesPlayed;
            flashlight.Toggle();
            yield return null;
            CheckThat("Flashlight toggling plays mechanical cue", utility.SwitchesPlayed == switches + 1);
            weapon.RestoreState(true, 0, weapon.Definition);
            int empty = utility.EmptyTriggersPlayed;
            weapon.TryFire();
            yield return null;
            CheckThat("Empty pistol plays a bounded dry trigger cue", utility.EmptyTriggersPlayed == empty + 1);
            for (int i = 0; i < 3; i++)
            {
                var material = (GroundSoundSurface)i;
                Vector2 start = new Vector2(120 + i * 20, 100);
                var floor = CreateProbeObject("Probe " + material);
                floor.transform.position = start;
                floor.AddComponent<BoxCollider2D>().size = new Vector2(8, 5);
                var region = floor.AddComponent<GroundAudioSurface2D>();
                Set(region, "surface", material);
                context.InjectScoped(region);
                playerBody.linearVelocity = Vector2.zero;
                playerBody.position = start;
                Physics2D.SyncTransforms();
                int before = feet.StepsPlayed;
                yield return new WaitForSeconds(.08f);
                CheckThat("Teleport does not fake a footstep " + material, feet.StepsPlayed == before);
                playerBody.linearVelocity = Vector2.right * 3.5f;
                yield return new WaitForSeconds(.85f);
                playerBody.linearVelocity = Vector2.zero;
                CheckThat("Actual travel resolves " + material + " footstep", feet.StepsPlayed > before && feet.LastSurface == material);
                yield return new WaitForSeconds(.1f);
                before = feet.StepsPlayed;
                yield return new WaitForSeconds(.25f);
                CheckThat("Standing still is silent " + material, feet.StepsPlayed == before);
            }

            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.1f);
            int sample = bed.timeSamples;
            yield return new WaitForSecondsRealtime(.3f);
            CheckThat("Game pause freezes ambient playback", !bed.isPlaying && Mathf.Abs(bed.timeSamples - sample) < 128);
            Time.timeScale = 1;
            yield return new WaitForSeconds(.25f);
            CheckThat("Resume preserves playing layers", bed.isPlaying && threat.isPlaying && combat.isPlaying);
            director.enabled = false;
            yield return null;
            CheckThat("Disable stops all scene audio layers", !bed.isPlaying && !threat.isPlaying && !combat.isPlaying);
            director.enabled = true;
            yield return new WaitForSeconds(.35f);
            CheckThat("Re-enable resets scene mood and restarts", bed.isPlaying && director.Mood == AudioMood.Explore);
            player.TakeDamage(99);
            int deathSteps = feet.StepsPlayed;
            yield return new WaitForSeconds(2f);
            CheckThat("Player death fades the bed and stops new footsteps", player.IsDead && director.LayerGains.magnitude < .02f && feet.StepsPlayed == deathSteps);
        }

        public void BeginRecording(string path)
        {
            folder = path;
            Directory.CreateDirectory(folder);
            mode = "recording";
            Prepare();
            previousCapture = Time.captureFramerate;
            previousBackground = Application.runInBackground;
            if (!AudioRenderer.Start())
            {
                throw new InvalidOperationException("Audio capture already active.");
            }

            capturing = true;
            Time.captureFramerate = 30;
            Application.runInBackground = true;
            writer = new BinaryWriter(File.Open(Path.Combine(folder, "unity-mix.f32"), FileMode.Create));
            director.enabled = false;
            director.enabled = true;
            StartCoroutine(Guard(Record()));
        }

        private IEnumerator Record()
        {
            while (Frames < 1080)
            {
                float at = Frames / 30f;
                if (at >= 2 && at < 5)
                {
                    playerBody.linearVelocity = Vector2.right * 3.5f;
                }
                else
                {
                    playerBody.linearVelocity = Vector2.zero;
                }

                if (Frames == 240)
                {
                    actor = Spawn((Vector2)player.transform.position + Vector2.right * 2.8f, true);
                    actor.GetComponent<EnemyHealth2D>().Initialize(10);
                    flashlight.SetPower(true);
                    flashlight.AimAt(actor.transform.position);
                }

                if (at >= 16 && shots < 4 && at >= 16 + shots * 1.5f)
                {
                    flashlight.AimAt(actor.transform.position);
                    weapon.TryFire();
                    shots++;
                }

                if (Frames == 720 && actor != null)
                {
                    actor.Kill();
                }

                if (Frames % 30 == 0)
                {
                    timeline.Add(at.ToString("F0") + ":" + director.Mood + ":" + director.LayerGains.ToString("F3"));
                }

                yield return new WaitForEndOfFrame();
                int samples = AudioRenderer.GetSampleCountForCaptureFrame();
                using (var b = new NativeArray<float>(samples * 2, Allocator.Temp))
                {
                    if (!AudioRenderer.Render(b))
                    {
                        throw new IOException("Unity audio render failed.");
                    }

                    foreach (float x in b)
                    {
                        writer.Write(x);
                        Peak = Mathf.Max(Peak, Mathf.Abs(x));
                    }
                }

                Frames++;
            }
        }

        private IEnumerator Guard(IEnumerator sequence)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!sequence.MoveNext())
                    {
                        break;
                    }

                    current = sequence.Current;
                }
                catch (Exception e)
                {
                    Failure = e.ToString();
                    break;
                }

                yield return current;
            }

            Finished = true;
            RestoreCapture();
            var report = new Report
            {
                failure = Failure,
                mode = mode,
                finished = Finished,
                frames = Frames,
                sampleRate = AudioSettings.outputSampleRate,
                peak = Peak,
                checks = checks.ToArray(),
                timeline = timeline.ToArray()
            };
            File.WriteAllText(Path.Combine(folder, "report.json"), JsonUtility.ToJson(report, true));
        }

        private void RestoreCapture()
        {
            if (!capturing)
            {
                return;
            }

            writer?.Dispose();
            writer = null;
            AudioRenderer.Stop();
            capturing = false;
            Time.captureFramerate = previousCapture;
            Application.runInBackground = previousBackground;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            RestoreCapture();
            RestoreTesting();
        }

        private void OnDestroy() => RestoreTesting();

        private void RestoreTesting()
        {
            if (ownsTimeScale)
            {
                Time.timeScale = previousTimeScale;
                ownsTimeScale = false;
            }

            if (testProfile != null)
            {
                if (director != null)
                {
                    Set(director, "profile", originalProfile);
                }

                Destroy(testProfile);
                testProfile = null;
            }
        }
    }
}
#endif
