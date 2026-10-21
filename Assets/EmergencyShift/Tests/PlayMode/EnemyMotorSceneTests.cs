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
    public sealed class EnemyMotorSceneTests
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
        public IEnumerator ScaledMotorSteersAndMovesOnTheFreeAxis()
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
            var prototype = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            Assert.That(prototype, Is.Not.Null);
            foreach (float scale in new[] { 2f, .5f, 1f })
            {
                Vector2 origin = new Vector2(220, 220);
                var actor = new GameObject("Scaled native motor", typeof(Rigidbody2D), typeof(CircleCollider2D));
                actor.layer = prototype.layer;
                actor.transform.position = origin;
                actor.transform.localScale = new Vector3(scale, scale, 1);
                var body = actor.GetComponent<Rigidbody2D>();
                body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                actor.GetComponent<CircleCollider2D>().radius = .4f;
                var motor = actor.AddComponent<EnemyMotor2D>();
                context.InjectScoped(motor);
                motor.Configure(1 << 8);
                var wall = new GameObject("Scaled steering wall");
                wall.layer = 8;
                wall.transform.position = origin + Vector2.right * (scale == .5f ? .6f : .9f);
                var wallShape = wall.AddComponent<BoxCollider2D>();
                wallShape.size = new Vector2(.1f, 4);
                Physics2D.SyncTransforms();
                Vector2 chosen = motor.ClearDirection(Vector2.right);
                Assert.That(chosen, Is.Not.EqualTo(Vector2.zero), $"scale={scale}: fixture must have a free direction");
                motor.Move(chosen * 1.5f, false);
                yield return new WaitForSeconds(.15f);
                Vector2 displacement = body.position - origin;
                if (scale == 2)
                {
                    Assert.That(displacement.y, Is.GreaterThan(.1f));
                    Assert.That(Mathf.Abs(displacement.x), Is.LessThan(.03f));
                }
                else
                {
                    Assert.That(displacement.x, Is.GreaterThan(.1f));
                    Assert.That(Mathf.Abs(displacement.y), Is.LessThan(.03f));
                }

                Assert.That(Physics2D.Distance(actor.GetComponent<Collider2D>(), wallShape).distance, Is.GreaterThanOrEqualTo(-.02f));
                Object.Destroy(actor);
                Object.Destroy(wall);
                yield return null;
            }
        }
    }
}
#endif
