using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AccessControl;
using EmergencyShift.Features.Pickups;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class InteractionActorPortTests
    {
        private GameObject owner;

        [TearDown]
        public void Cleanup() => UnityEngine.Object.DestroyImmediate(owner);

        [Test]
        public void CardNeedsReachAndSuccessfulGrantBeforeConsumption()
        {
            var pickup = Create<AccessCardPickup2D>();
            pickup.Construct(new PickupControllerFactory());
            var actor = new Actor { Reachable = false };
            Assert.That(pickup.TryCollect(actor), Is.False);
            Assert.That(actor.Grants, Is.Zero);
            actor.Reachable = true;
            actor.Accept = false;
            Assert.That(pickup.TryCollect(actor), Is.False);
            Assert.That(pickup.Collected, Is.False);
            actor.Accept = true;
            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(pickup.TryCollect(actor), Is.False);
            Assert.That(actor.Notices, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FlashlightRewardsWorkWithAComponentFreeRecipient(bool upgrade)
        {
            var actor = new Actor();
            IAutoPickup pickup;
            if (upgrade)
            {
                var component = Create<FlashlightUpgradePickup>();
                component.Construct(new PickupControllerFactory());
                pickup = component;
            }
            else
            {
                var component = Create<FlashlightPickup2D>();
                component.Construct(new PickupControllerFactory());
                pickup = component;
            }

            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(actor.Grants, Is.EqualTo(1));
            Assert.That(actor.Notices, Is.EqualTo(1));
        }

        [Test]
        public void PartialAmmoGrantKeepsRemainderWithoutConcreteWeaponAccess()
        {
            var pickup = Create<WeaponLootPickup>();
            pickup.Configure(WeaponLootPickup.LootKind.Ammo, 8);
            pickup.Construct(new PickupControllerFactory());
            var actor = new Actor { Capacity = 2 };
            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(pickup.RemainingCartridges, Is.EqualTo(6));
            Assert.That(pickup.Collected, Is.False);
            actor.Capacity = 0;
            Assert.That(pickup.TryCollect(actor), Is.False);
            Assert.That(pickup.RemainingCartridges, Is.EqualTo(6));
            actor.Capacity = 6;
            Assert.That(pickup.TryCollect(actor), Is.True);
            Assert.That(pickup.Collected, Is.True);
        }

        [Test]
        public void ReaderUsesCardCapabilityWithoutFetchingAPlayerComponent()
        {
            var access = Create<DoorAccess2D>();
            access.Construct(new DoorAccessControllerFactory(new UnityGameClock()));
            access.Configure("blue", "Синяя карта");
            var actor = new Actor { HasCard = false };
            Assert.That(access.TryUnlock(actor), Is.False);
            actor.HasCard = true;
            Assert.That(access.TryUnlock(actor), Is.True);
            Assert.That(access.IsUnlocked, Is.True);
        }

        [Test]
        public void DestroyedActorThroughInterfaceCannotCollectOrThrowMissingReference()
        {
            var pickup = Create<AccessCardPickup2D>();
            pickup.Construct(new PickupControllerFactory());
            var actorObject = new GameObject("Destroyed actor");
            IInteractionActor actor = actorObject.AddComponent<PlayerInteraction>();
            UnityEngine.Object.DestroyImmediate(actorObject);
            Assert.That(pickup.TryCollect(actor), Is.False);
            Assert.That(pickup.Collected, Is.False);
        }

        private T Create<T>() where T : Component
        {
            owner = new GameObject(typeof(T).Name);
            return owner.AddComponent<T>();
        }

        private sealed class Actor : IAccessCardRecipient, IFlashlightRecipient, IWeaponRecipient
        {
            public bool Reachable = true;
            public bool Accept = true;
            public bool HasCard = true;
            public int Capacity = 8;
            public int Grants;
            public int Notices;
            public bool CanReach(Vector2 point, float targetRange, Collider2D allowedBlocker = null) => Reachable;
            public void ShowNotification(string message) => Notices++;
            public void ReportPickup(PickupKind kind, string description) => Notices++;
            public bool HasAccessCard(string id) => HasCard;
            public bool TryCollectCard(string id) => Grant();
            public bool CanCollectFlashlight => true;
            public bool CanUpgradeFlashlight => true;
            public bool TryCollectFlashlight() => Grant();
            public bool TryCollectUpgrade(string id, string label, float rangeBonus, float angleBonus, bool repeatable) => Grant();
            public bool CanCollectWeapon => true;
            public bool HasWeapon => true;
            public int Ammo => 0;
            public int MaximumAmmo => Capacity;
            public string WeaponName => "Fixture";
            public int CollectAmmo(int amount, WeaponDefinition definition = null) => Math.Min(amount, Capacity);
            public bool CollectWeapon(int amount, WeaponDefinition definition) => Grant();

            private bool Grant()
            {
                Grants++;
                return Accept;
            }
        }
    }
}
