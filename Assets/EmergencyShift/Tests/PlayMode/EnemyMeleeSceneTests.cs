#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class EnemyMeleeSceneTests
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
        public IEnumerator BothAuthoredSpeciesRespectWallsAndDeliverOneRealDamagePerStrike()
        {
            previous = SceneManager.GetActiveScene();
            const string scenePath = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(scenePath);
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
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = new Vector2(101, 100);
            player.transform.position = new Vector3(101, 100, 0);
            context.Flashlight.SetPower(false);
            Physics2D.SyncTransforms();
            yield return null;
            foreach (string prefabName in new[] { "ShadowStalker", "Sleeper" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + prefabName + ".prefab");
                Assert.That(prefab, Is.Not.Null, prefabName);
                GameObject enemy = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity);
                var stalker = enemy.GetComponent<ShadowStalker2D>();
                if (stalker != null)
                {
                    stalker.Configure(player.transform, context.Flashlight, 1 << 8);
                    stalker.enabled = false;
                }
                else
                {
                    var sleeper = enemy.GetComponent<Sleeper2D>();
                    sleeper.Configure(player.transform, context.Flashlight, 1 << 8);
                    sleeper.enabled = false;
                }

                enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                var melee = enemy.GetComponent<EnemyMeleeAttack2D>();
                var wall = new GameObject("Temporary bite obstruction");
                wall.layer = 8;
                wall.transform.position = new Vector3(100.5f, 100, 0);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 3);
                Physics2D.SyncTransforms();
                int before = player.Health;
                int damageEvents = 0;
                Action<int, Vector2> damaged = (amount, direction) => damageEvents++;
                player.Damaged += damaged;
                try
                {
                    Assert.That(melee.CanStart(new Vector2(100, 100)), Is.False, prefabName);
                    melee.Begin();
                    melee.Step(1, new Vector2(100, 100), Vector2.right, stalker == null);
                    Assert.That(player.Health, Is.EqualTo(before), prefabName + " wall");
                    wall.SetActive(false);
                    Physics2D.SyncTransforms();
                    Assert.That(melee.CanStart(new Vector2(100, 100)), Is.True, prefabName);
                    melee.Begin();
                    melee.Step(1, new Vector2(100, 100), Vector2.right, stalker == null);
                    int after = player.Health;
                    Assert.That(after, Is.LessThan(before), prefabName + " actual damage");
                    melee.Step(1, new Vector2(100, 100), Vector2.right, stalker == null);
                    Assert.That(player.Health, Is.EqualTo(after));
                    Assert.That(damageEvents, Is.EqualTo(1));
                }
                finally
                {
                    player.Damaged -= damaged;
                }

                UnityEngine.Object.Destroy(enemy);
                UnityEngine.Object.Destroy(wall);
                yield return new WaitForSeconds(.8f);
            }
        }
    }
}
#endif
