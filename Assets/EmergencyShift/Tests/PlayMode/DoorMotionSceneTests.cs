#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class DoorMotionSceneTests
    {
        private Scene scene;
        private Scene previous;

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

        [UnityTest]
        public IEnumerator NativeLeafAndColliderFollowTimelineRestoreAndDisable()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            Door2D door = null;
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

                door = door != null ? door : root.GetComponentInChildren<Door2D>();
            }

            Assert.That(door, Is.Not.Null);
            var access = door.GetComponent<DoorAccess2D>();
            access?.RestoreState(true);
            door.RestoreState(false);
            Vector3 closed = door.Blocker.transform.localPosition;
            int starts = 0;
            int stops = 0;
            door.MotionStarted += open => starts++;
            door.MotionStopped += open => stops++;
            door.Toggle();
            Assert.That(door.IsMoving, Is.True);
            Time.timeScale = 0;
            Vector3 frozen = door.Blocker.transform.localPosition;
            yield return null;
            yield return null;
            Assert.That(door.Blocker.transform.localPosition, Is.EqualTo(frozen));
            Time.timeScale = 1;
            yield return new WaitForSeconds(.7f);
            Vector3 opened = door.Blocker.transform.localPosition;
            Assert.That(Vector3.Distance(opened, closed), Is.GreaterThan(.1f));
            Assert.That(door.IsMoving, Is.False);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
            door.RestoreState(false);
            Assert.That(door.Blocker.transform.localPosition, Is.EqualTo(closed));
            Assert.That(starts + stops, Is.EqualTo(2), "Restore does not replay motion/audio events.");
            door.Toggle();
            yield return null;
            door.enabled = false;
            Assert.That(door.IsMoving, Is.False);
            Assert.That(door.Blocker.transform.localPosition, Is.EqualTo(opened));
            Assert.That(stops, Is.EqualTo(2));
            door.enabled = true;
            yield return null;
            Assert.That(door.IsMoving, Is.False);
            door.RestoreState(false);
        }
    }
}
#endif
