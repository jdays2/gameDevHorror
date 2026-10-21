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
    public sealed class StalkerBrainSceneTests
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
        public IEnumerator RealStalkerKeepsDetectionFearBurnAggroHitAndLegacyRestoreRules()
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
            var wall = new GameObject("First detection obstruction");
            wall.layer = 8;
            wall.transform.position = new Vector3(100.5f, 100, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 3);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            GameObject enemy = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity);
            var stalker = enemy.GetComponent<ShadowStalker2D>();
            stalker.Configure(player.transform, context.Flashlight, 1 << 8);
            stalker.AttacksEnabled = false;
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.1f);
            Assert.That(stalker.HasAggro, Is.False);
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.06f);
            Assert.That(stalker.HasAggro, Is.True);
            playerBody.position = new Vector2(110, 100);
            player.transform.position = new Vector3(110, 100, 0);
            yield return new WaitForSeconds(.1f);
            Assert.That(stalker.HasAggro, Is.True, "Leaving detection radius cannot reset established aggro.");
            playerBody.position = new Vector2(101, 100);
            player.transform.position = new Vector3(101, 100, 0);
            context.Flashlight.AimAt(enemy.transform.position);
            context.Flashlight.SetPower(true);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.18f);
            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Sneaking));
            float deadline = Time.time + 1.5f;
            while (stalker.State != ShadowStalker2D.BehaviourState.Recoiling && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Recoiling));
            Assert.That(stalker.EyeDamageProgress, Is.GreaterThan(0));
            deadline = Time.time + 2;
            while (stalker.State != ShadowStalker2D.BehaviourState.Retreating && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Retreating));
            Assert.That(stalker.RetreatSpeed, Is.GreaterThan(stalker.PursuitSpeed));
            deadline = Time.time + 2;
            while (stalker.State != ShadowStalker2D.BehaviourState.Waiting && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Waiting));
            yield return new WaitForSeconds(stalker.BehaviourProfile.cooldownSeconds + .1f);
            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Waiting));
            float eyeDamage = stalker.EyeDamageProgress;
            context.Flashlight.SetPower(false);
            yield return new WaitForSeconds(.06f);
            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Sneaking));
            Assert.That(stalker.EyeDamageProgress, Is.EqualTo(eyeDamage));
            ShadowStalker2D.Snapshot saved = stalker.CaptureState();
            saved.hasEyeDamageProgress = false;
            saved.eyeBurned = true;
            saved.eyeDamage = 0;
            int states = 0;
            int burnCues = 0;
            stalker.StateChanged += _ => states++;
            stalker.EyeBurnStarted += () => burnCues++;
            stalker.RestoreState(saved);
            Assert.That(stalker.EyeDamageProgress, Is.EqualTo(1));
            Assert.That(states + burnCues, Is.Zero);
            stalker.AttacksEnabled = true;
            deadline = Time.time + 1.5f;
            while (stalker.State != ShadowStalker2D.BehaviourState.Attacking && Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Attacking));
            stalker.ApplyBulletHit(1, enemy.transform.position, Vector2.left);
            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Sneaking));
            Assert.That(stalker.IsStaggered, Is.True);
            Assert.That(stalker.CaptureState().melee.cooldown, Is.EqualTo(.35f).Within(.02f));
            stalker.Kill();
            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Dead));
            Assert.That(enemy.GetComponent<Rigidbody2D>().simulated, Is.False);
            Object.Destroy(enemy);
            Object.Destroy(wall);
            yield return null;
        }
    }
}
#endif
