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
    public sealed class EnemyPatrolSceneTests
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
        public IEnumerator AuthoredPatrolUsesScopedFactoryAndRealGeometryWithCheckpointState()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(scene);
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ShadowStalker2D brain in root.GetComponentsInChildren<ShadowStalker2D>(true))
                {
                    brain.enabled = false;
                }

                foreach (Sleeper2D brain in root.GetComponentsInChildren<Sleeper2D>(true))
                {
                    brain.enabled = false;
                }

                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
            }

            Assert.That(context, Is.Not.Null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            Vector2 origin = new Vector2(100, 100);
            GameObject enemy = context.CreateInstance(prefab, origin, Quaternion.identity);
            enemy.GetComponent<ShadowStalker2D>().enabled = false;
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            var patrol = enemy.GetComponent<EnemyPatrol2D>();
            var serialized = new SerializedObject(patrol);
            serialized.FindProperty("patrolEnabled").boolValue = true;
            serialized.FindProperty("areaSize").vector2Value = Vector2.one * 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            patrol.Initialize(origin);
            Assert.That(patrol.AreaOrigin, Is.EqualTo(origin));
            var state = new EnemyPatrol2D.Snapshot
            {
                origin = origin,
                hasTarget = true,
                target = origin + Vector2.right,
                previousDistance = 1
            };
            patrol.RestoreState(state);
            Physics2D.SyncTransforms();
            Vector2 clear = patrol.Step(origin, .3f, 1 << 8, .02f);
            Assert.That(clear.x, Is.GreaterThan(0));
            Assert.That(clear.y, Is.EqualTo(0).Within(.00001));
            var wall = new GameObject("Patrol next-step obstruction");
            wall.layer = 8;
            wall.transform.position = origin + Vector2.right * .3f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 2);
            Physics2D.SyncTransforms();
            Assert.That(patrol.Step(origin, .3f, 1 << 8, .02f), Is.EqualTo(Vector2.zero));
            Assert.That(patrol.HasTarget, Is.False);
            Assert.That(patrol.IsPausing, Is.True);
            patrol.RestoreState(state);
            EnemyPatrol2D.Snapshot restored = patrol.CaptureState();
            Assert.That(restored.target, Is.EqualTo(state.target));
            Assert.That(restored.hasTarget, Is.True);
            Assert.That(restored.pause, Is.Zero);
            Assert.That(patrol.ContainsBody(origin + Vector2.right * 2, .3f), Is.False);
            Object.Destroy(enemy);
            Object.Destroy(wall);
            yield return null;
        }
    }
}
#endif
