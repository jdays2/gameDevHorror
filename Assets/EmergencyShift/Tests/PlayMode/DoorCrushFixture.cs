#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Doors.Config;
using EmergencyShift.Features.Doors.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public abstract class DoorCrushFixture
    {
        protected const string Path = "Assets/EmergencyShift/Scenes/DoorCrushTest.unity";
        protected Scene scene;
        protected Scene previous;
        protected LevelContext2D context;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            previous = SceneManager.GetActiveScene();
            yield return Load();
            foreach (EnemyHealth2D enemy in All<EnemyHealth2D>())
            {
                enemy.gameObject.SetActive(false);
            }
            context.Player.SetHealthCapacity(100, 100);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        protected IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(Path);
            SceneManager.SetActiveScene(scene);
            foreach (LevelContext2D item in All<LevelContext2D>())
            {
                context = item;
            }
            Assert.That(context, Is.Not.Null);
        }

        protected T[] All<T>() where T : Component
        {
            var result = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }
            return result.ToArray();
        }

        protected Door2D Door(Vector2 point, Clock clock, bool crushing = true)
        {
            string name = crushing ? "CrushingDoor" : "Door";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/World/" + name + ".prefab");
            var door = context.CreateInstance(prefab, point, Quaternion.identity).GetComponent<Door2D>();
            // Test-only clock seam makes one-step sweeps and entry/exit deterministic; the real native adapter still executes.
            var field = typeof(Door2D).GetField("controller", BindingFlags.Instance | BindingFlags.NonPublic);
            ((DoorMotionController)field.GetValue(door)).Dispose();
            field.SetValue(door, new DoorMotionController(new DoorMotionModel(new DoorMotionSettings(1), clock), (IDoorMotionView)door));
            door.RestoreState(true);
            return door;
        }

        protected EnemyHealth2D Enemy(string species, Vector2 point)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
            GameObject actor = context.CreateInstance(prefab, point, Quaternion.identity);
            Freeze(actor);
            return actor.GetComponent<EnemyHealth2D>();
        }

        protected static void Freeze(GameObject actor)
        {
            foreach (Behaviour item in actor.GetComponents<Behaviour>())
            {
                if (item is ShadowStalker2D || item is Sleeper2D || item is CrowdAgent2D)
                {
                    item.enabled = false;
                }
            }
            var body = actor.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
            Physics2D.SyncTransforms();
        }

        protected static IEnumerator Finish(Door2D door, Clock clock)
        {
            clock.Time += 2;
            yield return null;
            yield return null;
            Assert.That(door.IsMoving, Is.False);
        }

        protected sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }
    }
}
#endif
