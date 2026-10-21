using System;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class WeaponLootPayloadTests
    {
        private GameObject owner;
        private WeaponLootPickup loot;
        private WeaponLootCheckpoint2D participant;

        [SetUp]
        public void Setup()
        {
            owner = new GameObject("Weapon loot payload fixture");
            owner.SetActive(false);
            loot = owner.AddComponent<WeaponLootPickup>();
            loot.Construct(new Features.Pickups.PickupControllerFactory());
            loot.Configure(WeaponLootPickup.LootKind.Ammo, 8);
            loot.RestoreState(true, 5);
            participant = owner.AddComponent<WeaponLootCheckpoint2D>();
        }

        [TearDown]
        public void Cleanup() => UnityEngine.Object.DestroyImmediate(owner);

        [TestCase("{\"format\":1,\"remaining\":3}")]
        [TestCase("{\"format\":1,\"consumed\":false}")]
        [TestCase("{\"format\":1,\"consumed\":true,\"remaining\":-1}")]
        [TestCase("{\"format\":1}")]
        public void PreflightRejectsMissingOrNegativeFields(string payload)
        {
            Assert.Throws<InvalidOperationException>(() => participant.ValidateCheckpoint(payload));
        }

        [TestCase("{\"format\":1,\"remaining\":3}")]
        [TestCase("{\"format\":1,\"consumed\":false}")]
        [TestCase("{\"format\":1,\"consumed\":true,\"remaining\":-1}")]
        [TestCase("{\"format\":1}")]
        [TestCase("{\"format\":2,\"consumed\":false,\"remaining\":3}")]
        public void DirectRestoreChecksPayloadBeforeChangingLoot(string payload)
        {
            Assert.Throws<InvalidOperationException>(() => participant.RestoreCheckpoint(payload));
            Assert.That(loot.Collected, Is.True);
            Assert.That(loot.RemainingCartridges, Is.EqualTo(5));
        }

        [TestCase(false, 0)]
        [TestCase(false, 7)]
        [TestCase(true, 0)]
        [TestCase(true, 8)]
        public void CompleteLegacyFormatOneRetainsExplicitFalseAndZero(bool consumed, int remaining)
        {
            string payload = "{\"format\":1,\"consumed\":" + (consumed ? "true" : "false") + ",\"remaining\":" + remaining + "}";
            Assert.DoesNotThrow(() => participant.ValidateCheckpoint(payload));
            participant.RestoreCheckpoint(payload);
            Assert.That(loot.Collected, Is.EqualTo(consumed));
            Assert.That(loot.RemainingCartridges, Is.EqualTo(remaining));
            string capture = participant.CaptureCheckpoint();
            Assert.DoesNotThrow(() => participant.ValidateCheckpoint(capture));
        }
    }
}
