using System;
using System.Collections;
using EmergencyShift.Features.AccessControl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class LevelObjectiveLifecycleTests
    {
        private Scene scene;
        private LevelDefinition definition;
        private LevelObjectiveController2D objectives;
        private PlayerAccessCards2D oldCards;
        private PlayerAccessCards2D newCards;
        private PlayerWeapon2D weapon;
        private PlayerWeapon2D newWeapon;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            scene = SceneManager.CreateScene("Objective lifetime " + Guid.NewGuid().ToString("N"));
            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            JsonUtility.FromJsonOverwrite("{\"levelId\":\"objective-lifetime\",\"objectives\":[{\"condition\":1,\"cardId\":\"service\",\"phase\":2,\"text\":\"Open door\"},{\"condition\":0,\"phase\":1,\"text\":\"Find card\"}]}", definition);
            oldCards = Cards("Old inventory");
            newCards = Cards("New inventory");
            weapon = Create("Inactive equipment", false).AddComponent<PlayerWeapon2D>();
            newWeapon = Create("New inactive equipment", false).AddComponent<PlayerWeapon2D>();
            GameObject owner = Create("Objectives", false);
            objectives = owner.AddComponent<LevelObjectiveController2D>();
            objectives.Construct(new Features.LevelProgress.LevelProgressFactory(new Core.Time.UnityGameClock()));
            objectives.Configure(definition, oldCards, weapon);
            owner.SetActive(true);
            yield return null;
            Assert.That(objectives.Phase, Is.EqualTo(1));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (scene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }

            UnityEngine.Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator NewInventoryUpdatesObjectivesAfterLiveReconfigure()
        {
            objectives.Configure(definition, newCards, weapon);
            objectives.Refresh();
            newCards.Add("service");
            Assert.That(objectives.Phase, Is.EqualTo(2));
            Assert.That(objectives.Text, Is.EqualTo("Open door"));
            yield break;
        }

        [UnityTest]
        public IEnumerator OldInventoryIsDetachedDuringLiveReconfigure()
        {
            objectives.Configure(definition, newCards, weapon);
            objectives.Refresh();
            int notifications = 0;
            objectives.Changed += Count;
            try
            {
                oldCards.Add("service");
                Assert.That(notifications, Is.Zero);
            }
            finally
            {
                objectives.Changed -= Count;
            }

            yield break;

            void Count() => notifications++;
        }

        [UnityTest]
        public IEnumerator DisableReenableAndRepeatedConfigureKeepOneSubscriptionPerSource()
        {
            objectives.Configure(definition, newCards, newWeapon);
            objectives.Configure(definition, newCards, newWeapon);
            int notifications = 0;
            objectives.Changed += Count;
            try
            {
                newCards.Add("service");
                Assert.That(notifications, Is.EqualTo(1));
                objectives.enabled = false;
                newCards.Restore(Array.Empty<string>());
                Assert.That(notifications, Is.EqualTo(1));
                objectives.enabled = true;
                Assert.That(objectives.Phase, Is.EqualTo(1));
                notifications = 0;
                newCards.Add("service");
                Assert.That(notifications, Is.EqualTo(1));
                Assert.That(objectives.Phase, Is.EqualTo(2));
            }
            finally
            {
                objectives.Changed -= Count;
            }

            yield break;

            void Count() => notifications++;
        }

        [UnityTest]
        public IEnumerator InvalidReconfigureDoesNotDetachThePreviousInventory()
        {
            var invalid = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => objectives.Configure(invalid, newCards, newWeapon));
                oldCards.Add("service");
                Assert.That(objectives.Phase, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.Destroy(invalid);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator ReconfiguredWeaponAndDestroyReleaseExactlyTheBoundSources()
        {
            objectives.Configure(definition, newCards, newWeapon);
            Assert.That(SubscriptionCount(weapon, "OwnershipChanged"), Is.Zero);
            Assert.That(SubscriptionCount(newWeapon, "OwnershipChanged"), Is.EqualTo(1));
            UnityEngine.Object.Destroy(objectives.gameObject);
            yield return null;
            Assert.That(SubscriptionCount(newWeapon, "OwnershipChanged"), Is.Zero);
            newCards.Add("service");
        }

        private static int SubscriptionCount(object source, string eventName)
        {
            var callback = (Delegate)source.GetType().GetField(eventName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(source);
            return callback?.GetInvocationList().Length ?? 0;
        }

        private PlayerAccessCards2D Cards(string name)
        {
            var inventory = Create(name, false).AddComponent<PlayerAccessCards2D>();
            inventory.Construct(new AccessControlFactory());
            return inventory;
        }

        private GameObject Create(string name, bool active)
        {
            var owner = new GameObject(name);
            owner.SetActive(active);
            SceneManager.MoveGameObjectToScene(owner, scene);
            return owner;
        }
    }
}
