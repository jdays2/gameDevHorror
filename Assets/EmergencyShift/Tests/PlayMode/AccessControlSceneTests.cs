#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class AccessControlSceneTests
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
        public IEnumerator RealLevelCompositionInjectsPersonalInventoriesAndReaders()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                context = root.GetComponentInChildren<LevelContext2D>(true);
                if (context != null)
                {
                    break;
                }
            }

            Assert.That(context, Is.Not.Null);
            var cards = context.Player.GetComponent<PlayerAccessCards2D>();
            Assert.That(cards.Add("scope-test-blue"), Is.True);
            var other = Create("Another composed actor");
            var otherCards = other.AddComponent<PlayerAccessCards2D>();
            context.InjectScoped(otherCards);
            Assert.That(otherCards.Has("scope-test-blue"), Is.False);
            DoorAccess2D reader = Reader(context, "Reader A");
            DoorAccess2D second = Reader(context, "Reader B");
            int feedback = 0;
            reader.AccessChecked += granted => feedback++;
            Assert.That(reader.TryUnlock(context.Player.GetComponent<PlayerInteraction>()), Is.True);
            Assert.That(reader.IsUnlocked, Is.True);
            Assert.That(second.IsUnlocked, Is.False);
            cards.Restore(null);
            Assert.That(reader.TryUnlock(context.Player.GetComponent<PlayerInteraction>()), Is.True);
            Assert.That(feedback, Is.EqualTo(1));
        }

        private DoorAccess2D Reader(LevelContext2D context, string name)
        {
            GameObject owner = Create(name);
            GameObject leaf = Create(name + " leaf");
            leaf.transform.SetParent(owner.transform, false);
            owner.AddComponent<Door2D>().Configure(leaf.transform);
            var access = owner.AddComponent<DoorAccess2D>();
            access.Configure("scope-test-blue", "Тестовая карта");
            context.InjectScoped(access);
            return access;
        }

        private GameObject Create(string name)
        {
            var owner = new GameObject(name);
            owner.SetActive(false);
            SceneManager.MoveGameObjectToScene(owner, scene);
            return owner;
        }
    }
}
#endif
