using System;
using System.Collections;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace EmergencyShift.Tests
{
    public sealed class InteractionSceneTests
    {
        private Scene scene;
        private PlayerInteraction actor;
        private GameplayInputBlock block;
        private readonly Vector2 origin = new Vector2(700, 700);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            scene = SceneManager.CreateScene("Interaction " + Guid.NewGuid().ToString("N"));
            var scope = new DiContainer();
            scope.Bind<IGameClock>().To<UnityGameClock>().AsSingle();
            PlayerInteractionInstaller.Install(scope);
            block = new GameplayInputBlock();
            GameObject hero = Create("Interaction actor", origin);
            hero.SetActive(false);
            actor = hero.AddComponent<PlayerInteraction>();
            actor.Construct(null, block, new SceneRegistry<Door2D>(), scope.Resolve<PlayerInteractionControllerFactory>(),
                new LocalIlluminationQuery2D(new SceneRegistry<LocalLamp2D>(), new SceneRegistry<ShotEffect2D>()));
            hero.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator NativeSelectionAndCommandRespectWallBlockAndDisable()
        {
            GameObject targetObject = Create("Interaction target", origin + Vector2.right);
            targetObject.AddComponent<CircleCollider2D>().isTrigger = true;
            var target = targetObject.AddComponent<InteractionProbeTarget>();
            Physics2D.SyncTransforms();
            actor.RefreshTarget();
            Assert.That(actor.CurrentTarget, Is.SameAs(target));
            Assert.That(actor.TryInteractCurrent(), Is.True);
            Assert.That(target.Calls, Is.EqualTo(1));
            using (block.Acquire())
            {
                Assert.That(actor.TryInteractCurrent(), Is.False);
                Assert.That(actor.CurrentTarget, Is.Null);
            }

            GameObject wall = Create("Interaction wall", origin + Vector2.right * .5f);
            wall.layer = 8;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 2);
            Physics2D.SyncTransforms();
            Assert.That(actor.TryInteractCurrent(), Is.False);
            Assert.That(target.Calls, Is.EqualTo(1));
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            actor.RefreshTarget();
            Assert.That(actor.CurrentTarget, Is.SameAs(target));
            actor.enabled = false;
            Assert.That(actor.CurrentTarget, Is.Null);
            Assert.That(actor.TryInteractCurrent(), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeDisableClearsActiveAndQueuedPickupNotices()
        {
            int collected = 0;
            actor.ItemCollected += kind => collected++;
            actor.ReportPickup(PickupKind.Ammo, "A");
            actor.ReportPickup(PickupKind.AccessCard, "B");
            Assert.That(collected, Is.EqualTo(2));
            Assert.That(actor.Notification, Is.EqualTo("Получено: A"));
            actor.enabled = false;
            Assert.That(actor.Notification, Is.Null);
            actor.enabled = true;
            yield return null;
            Assert.That(actor.Notification, Is.Null);
            actor.ShowNotification("C");
            Assert.That(actor.Notification, Is.EqualTo("C"));
        }

        private GameObject Create(string name, Vector2 position)
        {
            var value = new GameObject(name);
            SceneManager.MoveGameObjectToScene(value, scene);
            value.transform.position = position;
            return value;
        }
    }
}
