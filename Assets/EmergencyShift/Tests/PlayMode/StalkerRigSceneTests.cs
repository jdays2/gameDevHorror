#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using EmergencyShift.Features.Enemies.View;
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
        public IEnumerator BothAuthoredLevelsInheritRigAndMovingStatesKeepTheirLegs()
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
                    var actual = actor.GetComponent<StalkerLimbRig2D>();
                    Assert.That(actual, Is.Not.Null, actor.name);
                    Assert.That(actual.Gait, Is.Not.Null, actor.name);
                    actor.gameObject.SetActive(false);
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
                GameObject probe = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity);
                var stalker = probe.GetComponent<ShadowStalker2D>();
                var rig = probe.GetComponent<StalkerLimbRig2D>();
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
                Assert.That(rig.Gait.Phase, Is.GreaterThan(0));
                var surface = probe.transform.Find("Visual/Stalker articulated skin").GetComponent<MeshRenderer>();
                Assert.That(surface.enabled, Is.True);
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
                    Assert.That(surface.enabled, Is.True, state.ToString());
                    Assert.That(body.sprite, Is.Not.Null);
                    Assert.That(body.sprite.name.Contains("torso"), Is.True, state + " still draws the full old limbs");
                }
                stalker.Kill();
                yield return null;
                Assert.That(surface.enabled, Is.False, "Dead actor must not keep live legs.");
                Object.Destroy(probe);
                yield return SceneManager.UnloadSceneAsync(loaded);
                loaded = default;
            }
        }
    }
}
#endif
