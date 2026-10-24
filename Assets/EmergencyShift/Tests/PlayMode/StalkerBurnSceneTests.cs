#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using EmergencyShift.Features.ScriptedEncounters.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class StalkerBurnSceneTests
    {
        private Scene scene;
        private Scene previous;
        private LevelContext2D context;
        private ShadowStalker2D scripted;
        private readonly List<ShadowStalker2D> authored = new List<ShadowStalker2D>();

        [UnitySetUp]
        public IEnumerator Setup()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(scene);
            authored.Clear();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
                foreach (ShadowStalker2D actor in root.GetComponentsInChildren<ShadowStalker2D>(true))
                {
                    authored.Add(actor);
                    if (scripted == null && actor.GetComponent<ScriptedStalkerRoute2D>() != null)
                    {
                        scripted = actor;
                    }

                    actor.gameObject.SetActive(false);
                }
            }

            Assert.That(context, Is.Not.Null);
            Assert.That(scripted, Is.Not.Null);
            context.Player.SetHealthCapacity(100, 100);
            context.Player.GetComponent<PlayerMotor2D>().enabled = false;
            context.Player.GetComponent<PlayerAim2D>().enabled = false;
            context.Player.GetComponent<PlayerWeapon2D>().enabled = false;
            context.Flashlight.Acquire();
            context.Flashlight.SetPower(false);
            MovePlayer(new Vector2(101, 100));
            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }

            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }

            context = null;
            scripted = null;
        }

        [UnityTest]
        public IEnumerator OrdinaryStalkerUsesActualLightAndRestoresFearWithoutAnotherBurn()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            ShadowStalker2D actor = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity).GetComponent<ShadowStalker2D>();
            Prepare(actor);
            yield return CheckLightContact(actor);
            float damage = actor.EyeDamageProgress;
            var audio = actor.GetComponent<StalkerAudio2D>();
            int cues = audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn);
            foreach (ShadowStalker2D.BehaviourState state in new[] { ShadowStalker2D.BehaviourState.Recoiling, ShadowStalker2D.BehaviourState.Retreating, ShadowStalker2D.BehaviourState.Waiting })
            {
                ShadowStalker2D.Snapshot saved = actor.CaptureState();
                saved.state = state;
                saved.age = 0;
                saved.closure = 1;
                actor.RestoreState(saved);
                yield return new WaitForSeconds(.1f);
                Assert.That(actor.EyeDamageProgress, Is.EqualTo(damage));
                Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(cues));
                CheckSurface(actor, damage);
            }
        }

        [UnityTest]
        public IEnumerator ScriptedStalkerUsesTheSamePhysicalLightContact()
        {
            Prepare(scripted);
            yield return CheckLightContact(scripted);
            Assert.That(scripted.HasAggro, Is.False);
            Assert.That(scripted.GetComponent<ScriptedStalkerRoute2D>().ControlsActor, Is.True);
        }

        [UnityTest]
        public IEnumerator SecondLightReactionAfterWaitingPlaysBurnAgainEvenWithACompletelyBurnedEye()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            ShadowStalker2D actor = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity).GetComponent<ShadowStalker2D>();
            Prepare(actor);
            MovePlayer(new Vector2(101.5f, 100));
            var body = actor.GetComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var audio = actor.GetComponent<StalkerAudio2D>();
            SpriteRenderer pupil = System.Array.Find(actor.GetComponentsInChildren<SpriteRenderer>(true), value => value.name == "Eye pupil");
            Assert.That(pupil, Is.Not.Null);
            var samples = new List<TimelineSample>();
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(.08f);
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(1));
            Assert.That(audio.MovementSource.isPlaying, Is.True);
            Assert.That(audio.MovementSource.clip.name, Does.Contain("eye_burn"));
            Assert.That(audio.MovementSource.volume, Is.GreaterThan(0));
            Assert.That(actor.EyeDamageProgress, Is.Zero);
            float deadline = Time.time + 2;
            while (actor.State != ShadowStalker2D.BehaviourState.Recoiling && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Recoiling));
            float stoppedAt = Time.time;
            Vector2 stoppedPosition = body.position;
            float earlyDamage = actor.EyeDamageProgress;
            while (Time.time - stoppedAt < .85f)
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForEndOfFrame();
                Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Recoiling));
                Assert.That(body.linearVelocity.magnitude, Is.LessThan(.001f));
                Assert.That(Vector2.Distance(body.position, stoppedPosition), Is.LessThan(.01f));
                Assert.That(pupil.enabled, Is.True, "Whitening must remain visible through the stopped burn ramp.");
                CheckSurface(actor, actor.EyeDamageProgress);
                samples.Add(new TimelineSample { time = Time.time - stoppedAt, state = actor.State.ToString(),
                    damage = actor.EyeDamageProgress, pupil = pupil.enabled, speed = body.linearVelocity.magnitude });
            }

            Assert.That(actor.EyeDamageProgress, Is.GreaterThan(earlyDamage + .15f));
            Assert.That(actor.EyeDamageProgress, Is.LessThan(.5f));
            deadline = stoppedAt + 2;
            while (actor.MovementSpeed < .05f && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Retreating));
            Assert.That(Time.time - stoppedAt, Is.GreaterThanOrEqualTo(1.38f));
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            context.Flashlight.AimAt(actor.transform.position);
            yield return new WaitForSeconds(4.2f);
            Assert.That(actor.EyeDamageProgress, Is.EqualTo(1));
            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Waiting));
            context.Flashlight.SetPower(false);
            yield return new WaitForSeconds(actor.BehaviourProfile.CooldownSeconds + .1f);
            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Sneaking));
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(.08f);
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(2), "Owner reproduction: return after waiting must sound again.");
            Assert.That(audio.MovementSource.isPlaying, Is.True);
            Assert.That(audio.MovementSource.clip.name, Does.Contain("eye_burn"));
            yield return new WaitForSeconds(actor.ReactionDelay + .1f);
            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Recoiling));
            System.IO.Directory.CreateDirectory("Docs/Validation/StalkerBurnSequence02");
            System.IO.File.WriteAllText("Docs/Validation/StalkerBurnSequence02/native-timeline.json",
                JsonUtility.ToJson(new TimelineReport { secondCycleBurnCues = audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), samples = samples.ToArray() }, true));
        }

        [UnityTest]
        public IEnumerator EveryAuthoredAlphaStalkerStartsWithAnUndamagedEye()
        {
            Assert.That(authored.Count, Is.GreaterThanOrEqualTo(28));
            foreach (ShadowStalker2D actor in authored)
            {
                Assert.That(actor.EyeDamageProgress, Is.Zero, actor.name);
                Assert.That(actor.GetComponent<StalkerAudio2D>().PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.Zero, actor.name);
            }

            yield return null;
        }

        [System.Serializable]
        private sealed class TimelineSample
        {
            public float time;
            public string state;
            public float damage;
            public bool pupil;
            public float speed;
        }

        [System.Serializable]
        private sealed class TimelineReport
        {
            public int secondCycleBurnCues;
            public TimelineSample[] samples;
        }

        private void Prepare(ShadowStalker2D actor)
        {
            actor.gameObject.SetActive(true);
            actor.Configure(context.Player.transform, context.Flashlight, 1 << 8);
            actor.AttacksEnabled = false;
            var body = actor.GetComponent<Rigidbody2D>();
            body.position = new Vector2(100, 100);
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            actor.transform.position = new Vector3(100, 100, 0);
            Physics2D.SyncTransforms();
        }

        private IEnumerator CheckLightContact(ShadowStalker2D actor)
        {
            var audio = actor.GetComponent<StalkerAudio2D>();
            yield return new WaitForSeconds(.06f);
            Assert.That(actor.EyeDamageProgress, Is.Zero);
            context.Flashlight.AimAt(new Vector2(103, 100));
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(.2f);
            Assert.That(actor.EyeDamageProgress, Is.Zero, "A beam aimed away cannot burn the eye.");
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.Zero);
            var wall = new GameObject("BUG-047 physical beam obstruction");
            SceneManager.MoveGameObjectToScene(wall, scene);
            wall.layer = 8;
            wall.transform.position = new Vector3(100.5f, 100, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.15f, 3);
            context.Flashlight.AimAt(actor.transform.position);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.2f);
            Assert.That(actor.EyeDamageProgress, Is.Zero, "A wall blocks the contact, not just fear.");
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.Zero);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            Assert.That(actor.EyeDamageProgress, Is.Zero, "The contact sound precedes visible injury.");
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(1));
            yield return new WaitForSeconds(.2f);
            float damage = actor.EyeDamageProgress;
            Assert.That(damage, Is.InRange(.02f, .3f), "Injury is gradual, not a fear-state boolean.");
            wall.SetActive(true);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.2f);
            Assert.That(actor.EyeDamageProgress, Is.EqualTo(damage));
            CheckSurface(actor, damage);
            context.Flashlight.SetPower(false);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.1f);
            Assert.That(actor.EyeDamageProgress, Is.EqualTo(damage));
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(1));
        }

        private static void CheckSurface(ShadowStalker2D actor, float damage)
        {
            int layers = 0;
            foreach (SpriteRenderer renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader.name != "EmergencyShift/Localized Eye Burn")
                {
                    continue;
                }

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_BurnAmount"), Is.EqualTo(damage).Within(.00001), renderer.name);
                layers++;
            }

            Assert.That(layers, Is.EqualTo(2), "Both iris and pupil must use the personal dose.");
        }

        private void MovePlayer(Vector2 position)
        {
            var body = context.Player.GetComponent<Rigidbody2D>();
            body.position = position;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            context.Player.transform.position = position;
            Physics2D.SyncTransforms();
        }
    }
}
#endif
