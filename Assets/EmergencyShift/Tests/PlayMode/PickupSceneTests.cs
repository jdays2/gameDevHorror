#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class PickupSceneTests
    {
        private Scene scene;
        private Scene previous;
        private LevelContext2D context;
        private PlayerInteraction actor;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                context = root.GetComponentInChildren<LevelContext2D>(true);
                if (context != null)
                {
                    break;
                }
            }

            Assert.That(context, Is.Not.Null);
            actor = context.Player.GetComponent<PlayerInteraction>();
            yield return null;
        }

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
        public IEnumerator OpticsEventCannotConsumeTheSameUpgradeTwice()
        {
            var owner = new GameObject("Reentrant upgrade fixture");
            owner.SetActive(false);
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.transform.position = actor.transform.position;
            var pickup = owner.AddComponent<FlashlightUpgradePickup>();
            context.InjectScoped(pickup);
            var light = actor.Flashlight;
            bool entered = false;
            bool nested = false;
            int before = actor.Upgrades.InstalledCount;
            System.Action callback = () =>
            {
                if (!entered)
                {
                    entered = true;
                    nested = pickup.TryCollect(actor);
                }
            };
            light.OpticsChanged += callback;
            try
            {
                owner.SetActive(true);
                Physics2D.SyncTransforms();
                Assert.That(pickup.TryCollect(actor), Is.True);
                Assert.That(!nested && actor.Upgrades.InstalledCount == before + 1, Is.True,
                    $"Nested pickup={nested}, installed delta={actor.Upgrades.InstalledCount - before}; one scene instance must grant exactly one module.");
                Assert.That(pickup.Collected, Is.True);
            }
            finally
            {
                light.OpticsChanged -= callback;
            }

            var another = Pickup<FlashlightUpgradePickup>("Separate instance of same module type");
            Assert.That(another.TryCollect(actor), Is.True);
            Assert.That(actor.Upgrades.InstalledCount, Is.EqualTo(before + 2), "Different scene instances retain repeatable bonuses.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeCardChecksActorAndRestoresConsumptionQuietly()
        {
            var pickup = Pickup<AccessCardPickup2D>("Card fixture");
            pickup.Configure("pickup-once-test", "Test card");
            var cards = actor.GetComponent<PlayerAccessCards2D>();
            int notices = 0;
            actor.ItemCollected += kind => notices++;
            actor.enabled = false;
            Assert.That(pickup.TryCollect(actor), Is.False);
            Assert.That(pickup.Collected, Is.False);
            actor.enabled = true;
            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(cards.Has("pickup-once-test"), Is.True);
            Assert.That(pickup.TryCollect(actor), Is.False);
            pickup.RestoreState(true);
            Assert.That(notices, Is.EqualTo(1));
            Assert.That(pickup.Collected, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeFlashlightIsAcquiredOnceAndRestoreDoesNotReplayOwnership()
        {
            var pickup = Pickup<FlashlightPickup2D>("Flashlight fixture");
            Assert.That(actor.Flashlight.IsOwned, Is.False);
            int acquired = 0;
            System.Action callback = () => acquired++;
            actor.Flashlight.OwnershipChanged += callback;
            try
            {
                Assert.That(pickup.TryCollect(actor), Is.True);
                Assert.That(pickup.TryCollect(actor), Is.False);
                pickup.RestoreState(true);
                Assert.That(actor.Flashlight.IsOwned, Is.True);
                Assert.That(pickup.Collected, Is.True);
                Assert.That(acquired, Is.EqualTo(1));
            }
            finally
            {
                actor.Flashlight.OwnershipChanged -= callback;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeCappedUpgradeRemainsOnFloor()
        {
            Assert.That(actor.Upgrades.TryInstall("cap-test", "Cap", 100, 100, out _, true), Is.True);
            var pickup = Pickup<FlashlightUpgradePickup>("Capped upgrade fixture");
            Assert.That(pickup.TryCollect(actor), Is.False);
            Assert.That(pickup.Collected, Is.False);
            Assert.That(pickup.gameObject.activeSelf, Is.True);
            yield return null;
        }

        private T Pickup<T>(string name) where T : Component
        {
            var owner = new GameObject(name);
            owner.SetActive(false);
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.transform.position = actor.transform.position;
            var pickup = owner.AddComponent<T>();
            context.InjectScoped(pickup);
            owner.SetActive(true);
            Physics2D.SyncTransforms();
            return pickup;
        }

        [UnityTest]
        public IEnumerator NativeAmmoBoxKeepsPartialRemainderAndRestoresWithoutGivingAmmo()
        {
            var weapon = actor.Weapon;
            weapon.RestoreState(true, weapon.MaximumAmmo - 2, weapon.Definition);
            var pickup = Pickup<WeaponLootPickup>("Partial ammo fixture");
            pickup.Configure(WeaponLootPickup.LootKind.Ammo, 8, weapon.Definition);
            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(weapon.Ammo, Is.EqualTo(weapon.MaximumAmmo));
            Assert.That(pickup.RemainingCartridges, Is.EqualTo(6));
            Assert.That(pickup.Collected, Is.False);
            Assert.That(pickup.gameObject.activeSelf, Is.True);
            Assert.That(pickup.TryCollect(actor), Is.False);
            pickup.RestoreState(false, 3);
            Assert.That(weapon.Ammo, Is.EqualTo(weapon.MaximumAmmo));
            weapon.RestoreState(true, weapon.MaximumAmmo - 3, weapon.Definition);
            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(pickup.RemainingCartridges, Is.Zero);
            Assert.That(pickup.Collected, Is.True);
            Assert.That(pickup.gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeWeaponPickupAcquiresEmptyWeaponAndDuplicateRemains()
        {
            var pickup = Pickup<WeaponLootPickup>("Empty weapon fixture");
            pickup.Configure(WeaponLootPickup.LootKind.Weapon, 0, actor.Weapon.Definition);
            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(actor.Weapon.HasWeapon, Is.True);
            Assert.That(actor.Weapon.Ammo, Is.Zero);
            Assert.That(pickup.Collected, Is.True);
            var duplicate = Pickup<WeaponLootPickup>("Already owned weapon fixture");
            duplicate.Configure(WeaponLootPickup.LootKind.Weapon, 8, actor.Weapon.Definition);
            Assert.That(duplicate.TryCollect(actor), Is.False);
            Assert.That(duplicate.Collected, Is.False);
            Assert.That(duplicate.gameObject.activeSelf, Is.True);
            yield return null;
        }
    }
}
#endif
