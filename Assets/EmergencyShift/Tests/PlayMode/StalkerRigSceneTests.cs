#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class StalkerRigSceneTests
    {
        private Scene loaded;
        private Scene previous;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (loaded.IsValid() && loaded.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(loaded);
            }
        }

        [UnityTest]
        public IEnumerator BothAuthoredLevelsUseWholeSpritesAndFearDoesNotJumpToClosedBody()
        {
            previous = SceneManager.GetActiveScene();
            foreach (string level in new[] { "MechanicsSandbox", "MorgueRouteTest" })
            {
                string path = "Assets/EmergencyShift/Scenes/" + level + ".unity";
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                loaded = SceneManager.GetSceneByPath(path);
                SceneManager.SetActiveScene(loaded);
                LevelContext2D context = null;
                var actors = new List<ShadowStalker2D>();
                foreach (GameObject root in loaded.GetRootGameObjects())
                {
                    actors.AddRange(root.GetComponentsInChildren<ShadowStalker2D>(true));
                    if (context == null)
                    {
                        context = root.GetComponentInChildren<LevelContext2D>(true);
                    }
                }
                Assert.That(actors.Count, Is.EqualTo(level == "MechanicsSandbox" ? 10 : 28));
                yield return null;
                foreach (ShadowStalker2D actor in actors)
                {
                    var actual = actor.GetComponent<StalkerSpriteAnimator>();
                    Assert.That(actual, Is.Not.Null, actor.name);
                    Assert.That(actual.SpriteSet, Is.Not.Null, actor.name);
                    Assert.That(actor.GetComponentsInChildren<MeshRenderer>(true), Is.Empty, actor.name);
                    actor.gameObject.SetActive(false);
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
                GameObject probe = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity);
                var stalker = probe.GetComponent<ShadowStalker2D>();
                var animator = probe.GetComponent<StalkerSpriteAnimator>();
                var physics = probe.GetComponent<Rigidbody2D>();
                var body = probe.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
                ShadowStalker2D.Snapshot snapshot = stalker.CaptureState();
                snapshot.state = ShadowStalker2D.BehaviourState.Sneaking;
                snapshot.aggro = true;
                stalker.RestoreState(snapshot);
                stalker.enabled = false;
                physics.linearVelocity = Vector2.up * 1.5f;
                Vector2 start = physics.position;
                yield return new WaitForSeconds(.35f);
                Assert.That(physics.position.y - start.y, Is.GreaterThan(.3f));
                Assert.That(body.sprite.name, Does.StartWith("body-0-"));
                Assert.That(probe.GetComponentsInChildren<MeshRenderer>(true), Is.Empty);
                snapshot = stalker.CaptureState();
                snapshot.state = ShadowStalker2D.BehaviourState.Retreating;
                snapshot.closure = 1;
                stalker.RestoreState(snapshot);
                physics.linearVelocity = Vector2.up * 3.8f;
                yield return null;
                Assert.That(animator.DisplayedBodyFold, Is.LessThan(1), "Fear must not select the final compressed row immediately.");
                Assert.That(animator.CurrentFrame / animator.SpriteSet.Phases, Is.LessThan(animator.SpriteSet.ClosureRows - 1));
                yield return new WaitForSeconds(.35f);
                Assert.That(animator.DisplayedBodyFold, Is.EqualTo(animator.SpriteSet.CompactWalkingFold).Within(.001f));
                Assert.That(body.sprite.name, Does.StartWith("body-4-"));
                // The body responds to the recoil state while the eye is still open.
                snapshot.state = ShadowStalker2D.BehaviourState.Recoiling;
                snapshot.age = 0;
                snapshot.closure = 0;
                stalker.RestoreState(snapshot);
                stalker.enabled = false; // Restore re-enables the brain; hold this diagnostic state.
                physics.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(.3f);
                Assert.That(animator.EyeOpen, Is.True);
                Assert.That(animator.DisplayedBodyFold, Is.EqualTo(1).Within(.001f));
                Assert.That(body.sprite.name, Does.StartWith("body-8-"));
                foreach (ShadowStalker2D.BehaviourState state in new[] {
                    ShadowStalker2D.BehaviourState.Recoiling, ShadowStalker2D.BehaviourState.Retreating,
                    ShadowStalker2D.BehaviourState.Waiting, ShadowStalker2D.BehaviourState.Attacking })
                {
                    snapshot = stalker.CaptureState();
                    snapshot.state = state;
                    snapshot.age = .1f;
                    stalker.RestoreState(snapshot);
                    physics.linearVelocity = state == ShadowStalker2D.BehaviourState.Retreating ? Vector2.right * 3.8f : Vector2.zero;
                    yield return new WaitForSeconds(.2f);
                    Assert.That(body.sprite, Is.Not.Null);
                    Assert.That(body.sprite.name.Contains("torso"), Is.False, state + " draws only a cut torso");
                    Assert.That(probe.GetComponentsInChildren<MeshRenderer>(true), Is.Empty);
                }
                stalker.Kill();
                yield return null;
                Assert.That(animator.EyeOpen, Is.False);
                Object.Destroy(probe);
                yield return SceneManager.UnloadSceneAsync(loaded);
                loaded = default;
            }
        }
    }
}
#endif
