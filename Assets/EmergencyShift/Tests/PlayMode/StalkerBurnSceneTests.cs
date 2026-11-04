#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                yield return new WaitForEndOfFrame();
                Assert.That(actor.EyeDamageProgress, Is.LessThanOrEqualTo(damage));
                Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(cues));
                CheckSurface(actor, actor.EyeDamageProgress);
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
        public IEnumerator ApproachWarnsBeforeCloseBurnThenTwitchesRetreatsRecoversAndReactsAgain()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            ShadowStalker2D actor = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity).GetComponent<ShadowStalker2D>();
            Prepare(actor);
            MovePlayer(new Vector2(102.8f, 100));
            var body = actor.GetComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var audio = actor.GetComponent<StalkerAudio2D>();
            SpriteRenderer pupil = System.Array.Find(actor.GetComponentsInChildren<SpriteRenderer>(true), value => value.name == "Eye pupil");
            float restingPupil = pupil.transform.localScale.x;
            var samples = new List<TimelineSample>();
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            Vector2 start = body.position;
            yield return new WaitForSeconds(.2f);
            Assert.That(body.position.x, Is.GreaterThan(start.x), "The creature keeps approaching during warning.");
            Assert.That(pupil.transform.localScale.x, Is.GreaterThan(restingPupil));
            Assert.That(actor.EyeDamageProgress, Is.Zero);
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.Zero);
            float deadline = Time.time + 3;
            while (audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn) == 0 && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(1));
            Assert.That(audio.MovementSource.isPlaying, Is.True);
            Assert.That(audio.MovementSource.clip.name, Does.Contain("eye_burn"));
            Assert.That(audio.MovementSource.volume, Is.GreaterThan(0));
            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Recoiling));
            float stoppedAt = Time.time;
            Vector2 stoppedPosition = body.position;
            while (Time.time - stoppedAt < .09f)
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForEndOfFrame();
                Assert.That(body.linearVelocity.magnitude, Is.LessThan(.001f));
                Assert.That(Vector2.Distance(body.position, stoppedPosition), Is.LessThan(.01f));
                Assert.That(pupil.enabled, Is.True);
                CheckSurface(actor, actor.EyeDamageProgress);
                samples.Add(new TimelineSample { time = Time.time - stoppedAt, state = actor.State.ToString(),
                    damage = actor.EyeDamageProgress, pupil = pupil.enabled, speed = body.linearVelocity.magnitude });
            }

            Assert.That(actor.EyeDamageProgress, Is.GreaterThan(.2f));
            deadline = stoppedAt + .7f;
            while (actor.MovementSpeed < .05f && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Retreating));
            Assert.That(Time.time - stoppedAt, Is.InRange(.17f, .4f));
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            context.Flashlight.SetPower(false);
            float injured = actor.EyeDamageProgress;
            yield return new WaitForSeconds(2);
            Assert.That(actor.EyeDamageProgress, Is.LessThan(injured));
            Assert.That(actor.EyeDamageProgress, Is.GreaterThan(0));
            yield return new WaitForSeconds(9);
            Assert.That(actor.EyeDamageProgress, Is.Zero);
            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Sneaking));
            MovePlayer(body.position + Vector2.right * 1.5f);
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(actor.ReactionDelay + .08f);
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(2));
            Assert.That(audio.MovementSource.isPlaying, Is.True);
            Assert.That(actor.EyeDamageProgress, Is.GreaterThan(0));
            string output = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Logs", "Tests", "StalkerBurn", "native-light-timeline.json"));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            System.IO.File.WriteAllText(output,
                JsonUtility.ToJson(new TimelineReport { secondCycleBurnCues = audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), samples = samples.ToArray() }, true));
        }

        [UnityTest]
        public IEnumerator BothSpeciesKeepSoftFleshAlternativesAndFrozenEyeSurfaceThroughRestore()
        {
            int index = 0;
            foreach (string species in new[] { "ShadowStalker", "Sleeper" })
            {
                foreach (bool withEye in new[] { true, false })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
                    GameObject actor = context.CreateInstance(prefab, new Vector3(500 + index * 15, 500, 0), Quaternion.identity);
                    actor.name = "Eye03-" + species + "-" + withEye;
                    var burst = actor.GetComponent<GoreBurst2D>();
                    var fields = new SerializedObject(burst);
                    var original = (GoreProfile)fields.FindProperty("profile").objectReferenceValue;
                    var profile = Object.Instantiate(original);
                    string suffix = withEye ? "-eye03" : "-flesh03";
                    GoreVariant selected = Enumerable.Range(0, original.VariantCount).Select(original.VariantAt)
                        .First(value => !value.RestorationOnly && value.Id.EndsWith(suffix, System.StringComparison.Ordinal));
                    JsonUtility.FromJsonOverwrite("{\"variants\":[" + JsonUtility.ToJson(selected) + "]}", profile);
                    fields.FindProperty("profile").objectReferenceValue = profile;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                    float expectedDamage = 0;
                    var stalker = actor.GetComponent<ShadowStalker2D>();
                    if (stalker != null)
                    {
                        ShadowStalker2D.Snapshot state = stalker.CaptureState();
                        state.hasEyeDamageProgress = true;
                        state.eyeDamage = .65f;
                        stalker.RestoreState(state);
                        stalker.enabled = false;
                        expectedDamage = .65f;
                    }

                    actor.GetComponent<EnemyHealth2D>().Kill();
                    yield return null;
                    float deadline = Time.realtimeSinceStartup + 8;
                    while (!burst.IsSettled && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }

                    Assert.That(burst.IsSettled, Is.True);
                    RemainsSnapshot saved = burst.CaptureRemains();
                    Assert.That(saved.version, Is.EqualTo(RemainsSnapshot.CurrentVersion));
                    Assert.That(saved.parts.Count(part => part.hasEyeSurface), Is.EqualTo(withEye ? 1 : 0));
                    foreach (RemainsPartState part in saved.parts)
                    {
                        Assert.That(part.body.color, Is.EqualTo(profile.SettledTint));
                        Assert.That(Mathf.Max(part.body.color.r, part.body.color.g, part.body.color.b), Is.LessThan(1));
                        if (part.hasEyeSurface)
                        {
                            Assert.That(part.eyeDamage, Is.EqualTo(expectedDamage));
                        }
                    }

                    if (!withEye)
                    {
                        Assert.That(saved.parts.Any(part => part.spriteIndex == 7 || part.spriteIndex == 9), Is.False);
                        Assert.That(saved.parts.Count(part => part.spriteIndex >= 10), Is.EqualTo(10));
                    }

                    GameObject restoredActor = context.CreateInstance(prefab, actor.transform.position, Quaternion.identity);
                    restoredActor.name = "Restored-" + actor.name;
                    var restored = restoredActor.GetComponent<GoreBurst2D>();
                    restored.RestoreRemains(saved);
                    RemainsSnapshot roundTrip = restored.CaptureRemains();
                    Assert.That(roundTrip.parts.Length, Is.EqualTo(saved.parts.Length));
                    for (int part = 0; part < saved.parts.Length; part++)
                    {
                        Assert.That(roundTrip.parts[part].spriteIndex, Is.EqualTo(saved.parts[part].spriteIndex));
                        Assert.That(roundTrip.parts[part].hasEyeSurface, Is.EqualTo(saved.parts[part].hasEyeSurface));
                        Assert.That(roundTrip.parts[part].eyeDamage, Is.EqualTo(saved.parts[part].eyeDamage));
                        Assert.That(roundTrip.parts[part].body.color, Is.EqualTo(saved.parts[part].body.color));
                    }

                    GameObject record = scene.GetRootGameObjects().Single(root => root.name == "Persistent remains — " + restoredActor.name);
                    foreach (RemainsPartIdentity2D identity in record.GetComponentsInChildren<RemainsPartIdentity2D>())
                    {
                        if (!identity.Capture().hasEyeSurface)
                        {
                            continue;
                        }

                        var properties = new MaterialPropertyBlock();
                        SpriteRenderer renderer = identity.GetComponentInChildren<SpriteRenderer>();
                        renderer.GetPropertyBlock(properties);
                        Assert.That(properties.GetFloat("_BurnAmount"), Is.EqualTo(expectedDamage));
                        Assert.That(renderer.sharedMaterial, Is.SameAs(profile.EyeSurfaceMaterial));
                    }

                    Object.Destroy(profile);
                    index++;
                }
            }
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
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.Zero, "Warning before the close threshold is silent.");
            yield return new WaitForSeconds(actor.ReactionDelay + .06f);
            Assert.That(audio.PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.EqualTo(1));
            float damage = actor.EyeDamageProgress;
            Assert.That(damage, Is.InRange(.05f, .9f), "Injury is gradual, not a fear-state boolean.");
            wall.SetActive(true);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.2f);
            Assert.That(actor.EyeDamageProgress, Is.LessThan(damage), "Outside the beam the eye recovers.");
            yield return new WaitForEndOfFrame();
            CheckSurface(actor, actor.EyeDamageProgress);
            context.Flashlight.SetPower(false);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.1f);
            Assert.That(actor.EyeDamageProgress, Is.LessThan(damage));
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
