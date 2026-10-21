#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class SleeperBrainSceneTests
    {
        private Scene scene;
        private Scene previous;

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
        }

        [UnityTest]
        public IEnumerator RealSleeperWakesToBeamKeepsAggroCommitsLungeAndRestoresSilently()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(scene);
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ShadowStalker2D actor in root.GetComponentsInChildren<ShadowStalker2D>(true))
                {
                    actor.gameObject.SetActive(false);
                }

                foreach (Sleeper2D actor in root.GetComponentsInChildren<Sleeper2D>(true))
                {
                    actor.gameObject.SetActive(false);
                }

                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
            }

            Assert.That(context, Is.Not.Null);
            PlayerLife2D player = context.Player;
            player.SetHealthCapacity(100, 100);
            player.GetComponent<PlayerMotor2D>().enabled = false;
            var playerBody = player.GetComponent<Rigidbody2D>();
            playerBody.position = new Vector2(101, 100);
            playerBody.linearVelocity = Vector2.zero;
            player.transform.position = new Vector3(101, 100, 0);
            context.Flashlight.SetPower(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab");
            GameObject enemy = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity);
            var sleeper = enemy.GetComponent<Sleeper2D>();
            sleeper.Configure(player.transform, context.Flashlight, 1 << 8);
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            yield return new WaitForSeconds(.1f);
            Assert.That(sleeper.State, Is.EqualTo(Sleeper2D.BehaviourState.Sleeping));
            Assert.That(sleeper.HasAggro, Is.False, "Proximity alone does not wake this species.");
            context.Flashlight.AimAt(enemy.transform.position);
            context.Flashlight.SetPower(true);
            var wall = new GameObject("Beam obstruction");
            wall.layer = 8;
            wall.transform.position = new Vector3(100.5f, 100, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 3);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.8f);
            Assert.That(sleeper.State, Is.EqualTo(Sleeper2D.BehaviourState.Sleeping), "Wall blocks beam exposure.");
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            float deadline = Time.time + 2.5f;
            while (!sleeper.HasAggro && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(sleeper.HasAggro, Is.True);
            Assert.That(sleeper.State, Is.EqualTo(Sleeper2D.BehaviourState.Waking));
            context.Flashlight.SetPower(false);
            deadline = Time.time + 2.5f;
            while (sleeper.State != Sleeper2D.BehaviourState.Attacking && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(sleeper.State, Is.EqualTo(Sleeper2D.BehaviourState.Attacking));
            Vector2 heading = sleeper.AttackHeading;
            Assert.That(heading.x, Is.GreaterThan(.9f));
            playerBody.position = new Vector2(100, 101);
            player.transform.position = new Vector3(100, 101, 0);
            Physics2D.SyncTransforms();
            int health = player.Health;
            deadline = Time.time + 1.5f;
            while (sleeper.State == Sleeper2D.BehaviourState.Attacking && Time.time < deadline)
            {
                Assert.That(sleeper.AttackHeading, Is.EqualTo(heading));
                yield return null;
            }

            Assert.That(sleeper.State, Is.EqualTo(Sleeper2D.BehaviourState.Recovering));
            Assert.That(player.Health, Is.EqualTo(health), "Committed attack cannot turn into a sideways bite.");
            Assert.That(sleeper.HasAggro, Is.True, "Turning the beam off cannot erase aggro.");
            Sleeper2D.Snapshot snapshot = sleeper.CaptureState();
            int states = 0;
            sleeper.StateChanged += _ => states++;
            sleeper.RestoreState(snapshot);
            Assert.That(states, Is.Zero);
            Assert.That(sleeper.CaptureState().heading, Is.EqualTo(snapshot.heading));
            sleeper.ReceiveBulletHit(99, enemy.transform.position, Vector2.right);
            Assert.That(sleeper.State, Is.EqualTo(Sleeper2D.BehaviourState.Dead));
            Assert.That(enemy.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(states, Is.EqualTo(1));
            Object.Destroy(enemy);
            Object.Destroy(wall);
            yield return null;
        }
    }
}
#endif
