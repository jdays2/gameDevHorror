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
    public sealed class EnemyNavigationSceneTests
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
        public IEnumerator RealAggroStalkerNavigatesAroundWallWithoutCrossingIt()
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
            playerBody.position = new Vector2(103, 100);
            player.transform.position = new Vector3(103, 100, 0);
            var wall = new GameObject("Navigation detour obstruction");
            wall.layer = 8;
            wall.transform.position = new Vector3(101.3f, 100, 0);
            var wallShape = wall.AddComponent<BoxCollider2D>();
            wallShape.size = new Vector2(.2f, 2);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            GameObject enemy = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity);
            var stalker = enemy.GetComponent<ShadowStalker2D>();
            stalker.Configure(player.transform, context.Flashlight, 1 << 8);
            stalker.AttacksEnabled = false;
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
            var shape = enemy.GetComponent<CircleCollider2D>();
            Physics2D.SyncTransforms();
            stalker.ApplyBulletHit(1, enemy.transform.position, Vector2.left);
            Assert.That(stalker.HasAggro, Is.True);
            bool detoured = false;
            float deadline = Time.time + 12;
            while (Vector2.Distance(enemy.transform.position, player.transform.position) > 1.3f && Time.time < deadline)
            {
                detoured |= Mathf.Abs(enemy.transform.position.y - 100) > .9f;
                Assert.That(Physics2D.Distance(shape, wallShape).distance, Is.GreaterThanOrEqualTo(-.02f));
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Vector2.Distance(enemy.transform.position, player.transform.position), Is.LessThanOrEqualTo(1.3f));
            Assert.That(detoured, Is.True, "The solid wall requires a real detour, not a direct charge.");
            Assert.That(stalker.HasAggro, Is.True);
            Assert.That(stalker.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Sneaking));
            Object.Destroy(enemy);
            Object.Destroy(wall);
            yield return null;
        }
    }
}
#endif
