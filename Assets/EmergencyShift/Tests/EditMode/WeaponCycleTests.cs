using System;
using EmergencyShift.Core.Randomness;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class WeaponCycleTests
    {
        private PumpShotgunDefinition shotgun;
        private WeaponDefinition pistol;
        private WeaponCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            shotgun = ScriptableObject.CreateInstance<PumpShotgunDefinition>();
            JsonUtility.FromJsonOverwrite("{\"contentId\":\"test-shotgun\",\"selectionSlot\":1,\"secondsBetweenShots\":0.9,\"maximumAmmo\":36}", shotgun);
            pistol = ScriptableObject.CreateInstance<WeaponDefinition>();
            JsonUtility.FromJsonOverwrite("{\"contentId\":\"test-pistol\"}", pistol);
            catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            var data = new SerializedObject(catalog);
            SerializedProperty definitions = data.FindProperty("weapons");
            definitions.arraySize = 2;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = pistol;
            definitions.GetArrayElementAtIndex(1).objectReferenceValue = shotgun;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(shotgun);
            UnityEngine.Object.DestroyImmediate(pistol);
            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void FourShellsRequireFourPressesAndFourPumpCycles()
        {
            var cycle = shotgun.CreateCycle(12);
            int pumps = 0;
            cycle.Operation += op => pumps += op == WeaponOperation.Pump ? 1 : 0;
            for (int i = 0; i < 4; i++)
            {
                float time = i * 1.1f;
                Assert.That(cycle.TryFire(time), Is.True);
                Assert.That(cycle.TryFire(time + .1f), Is.False);
                Assert.That(cycle.TryReload(time + .1f), Is.False);
                Assert.That(cycle.CanHolster, Is.False);
                cycle.Tick(time + 1);
                cycle.Tick(time + 1.01f);
            }

            Assert.That(pumps, Is.EqualTo(4));
            Assert.That(cycle.Loaded, Is.Zero);
            Assert.That(cycle.TotalAmmo, Is.EqualTo(8));
            Assert.That(cycle.TryFire(5), Is.False);
        }

        [Test]
        public void ReloadCommitsOneShellAndInterruptedInsertDoesNotCreateAmmo()
        {
            var cycle = shotgun.CreateCycle(12);
            Assert.That(cycle.TryFire(0), Is.True);
            cycle.Tick(1);
            Assert.That(cycle.TryFire(1.1f), Is.True);
            cycle.Tick(2.1f);
            Assert.That(cycle.TryReload(3), Is.True);
            cycle.Tick(3.4f);
            Assert.That(cycle.Loaded, Is.EqualTo(2));
            cycle.Tick(3.81f);
            Assert.That(cycle.Loaded, Is.EqualTo(3));
            Assert.That(cycle.TryFire(3.9f), Is.True);
            cycle.Tick(10);
            Assert.That(cycle.Phase, Is.EqualTo(WeaponPhase.Ready));
            Assert.That(cycle.Loaded, Is.EqualTo(2));
            Assert.That(cycle.TotalAmmo, Is.EqualTo(9));
        }

        [Test]
        public void SharedDefinitionKeepsIndependentAmmoAndReloadState()
        {
            var first = shotgun.CreateCycle(12);
            var second = shotgun.CreateCycle(12);
            first.TryFire(0);
            first.Tick(1);
            first.TryReload(1);
            Assert.That(second.TotalAmmo, Is.EqualTo(12));
            Assert.That(second.Loaded, Is.EqualTo(4));
            Assert.That(second.Phase, Is.EqualTo(WeaponPhase.Ready));
            Assert.That(shotgun.MagazineCapacity, Is.EqualTo(4));
        }

        [Test]
        public void TypedAmmoAndSwitchingPreserveEachOwnedWeapon()
        {
            var inventory = new WeaponInventory();
            Assert.That(inventory.AddAmmo(10, shotgun.ToSettings()), Is.Zero);
            inventory.Acquire(pistol.ToSettings(), 7);
            inventory.ActiveCycle.TryFire(0);
            inventory.Acquire(shotgun.ToSettings(), 12);
            inventory.ActiveCycle.TryFire(0);
            Assert.That(inventory.Equip(0), Is.False);
            inventory.ActiveCycle.Tick(1);
            inventory.ActiveCycle.TryReload(1);
            inventory.AddAmmo(2, pistol.ToSettings());
            Assert.That(inventory.Equip(0), Is.True);
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(8));
            Assert.That(inventory.Equip(1), Is.True);
            inventory.ActiveCycle.Tick(5);
            Assert.That(inventory.ActiveCycle.Loaded, Is.EqualTo(3));
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(11));
        }

        [Test]
        public void JsonRoundTripPreservesPendingShellWithoutRepeatingPump()
        {
            var inventory = new WeaponInventory();
            inventory.Acquire(pistol.ToSettings(), 7);
            inventory.Acquire(shotgun.ToSettings(), 12);
            inventory.ActiveCycle.TryFire(0);
            inventory.ActiveCycle.Tick(1);
            inventory.ActiveCycle.TryReload(1);
            var saved = inventory.Capture(1.4f);
            var decoded = JsonUtility.FromJson<WeaponInventorySnapshot>(JsonUtility.ToJson(saved));
            var restored = new WeaponInventory();
            restored.Restore(decoded, catalog.ToSettings(), 10);
            int cues = 0;
            restored.ActiveCycle.Operation += op => cues++;
            restored.ActiveCycle.Tick(10.2f);
            Assert.That(restored.ActiveCycle.Loaded, Is.EqualTo(3));
            restored.ActiveCycle.Tick(10.5f);
            Assert.That(restored.ActiveCycle.Loaded, Is.EqualTo(4));
            Assert.That(restored.ActiveCycle.TotalAmmo, Is.EqualTo(11));
            Assert.That(cues, Is.EqualTo(1));
            restored.Equip(0);
            Assert.That(restored.ActiveCycle.TotalAmmo, Is.EqualTo(7));
        }

        [Test]
        public void InvalidInventoryIsRejectedBeforeChangingLiveWeapon()
        {
            var inventory = new WeaponInventory();
            inventory.Acquire(pistol.ToSettings(), 7);
            var saved = inventory.Capture(0);
            saved.weapons[0].contentId = "unknown";
            Assert.Throws<InvalidOperationException>(() => inventory.Restore(saved, catalog.ToSettings(), 0));
            Assert.That(inventory.ActiveDefinition.ContentId, Is.EqualTo(pistol.ContentId));
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(7));
            Assert.Throws<InvalidOperationException>(() => WeaponInventory.Validate(JsonUtility.FromJson<WeaponInventorySnapshot>("{}"), catalog.ToSettings()));
        }

        [Test]
        public void FullMagazineOrEmptyReserveCannotReloadOrGainFreeShells()
        {
            var cycle = shotgun.CreateCycle(4);
            Assert.That(cycle.TryReload(0), Is.False);
            cycle.TryFire(0);
            cycle.Tick(1);
            Assert.That(cycle.TryReload(1), Is.False);
            Assert.That(cycle.AddAmmo(3), Is.EqualTo(3));
            Assert.That(cycle.Loaded, Is.EqualTo(3));
            Assert.That(cycle.TryReload(2), Is.True);
            cycle.Tick(3);
            Assert.That(cycle.Loaded, Is.EqualTo(4));
            Assert.That(cycle.TotalAmmo, Is.EqualTo(6));
        }

        [Test]
        public void DefinitionReplacementRejectsCollisionWithoutLosingEitherWeapon()
        {
            var inventory = new WeaponInventory();
            inventory.Acquire(pistol.ToSettings(), 7);
            inventory.Acquire(shotgun.ToSettings(), 12);
            Assert.Throws<InvalidOperationException>(() => inventory.ReplaceActiveDefinition(pistol.ToSettings()));
            Assert.That(inventory.Count, Is.EqualTo(2));
            Assert.That(inventory.ActiveDefinition.ContentId, Is.EqualTo(shotgun.ContentId));
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(12));
            Assert.That(inventory.Equip(0), Is.True);
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(7));
        }

        [Test]
        public void PelletDirectionsStayInSpreadWithoutOrderedAngularSlots()
        {
            var previous = UnityEngine.Random.state;
            try
            {
                var random = new SystemRandomSource(new System.Random(4721));
                WeaponSettings settings = shotgun.ToSettings();
                float previousAngle = -180;
                bool hasUnorderedPair = false;
                for (int index = 0; index < shotgun.ProjectileCount; index++)
                {
                    Vector2 direction = settings.ProjectileDirection(Vector2.up, index, random);
                    float angle = Vector2.SignedAngle(Vector2.up, direction);
                    Assert.That(direction.magnitude, Is.EqualTo(1).Within(.0001f));
                    Assert.That(Mathf.Abs(angle), Is.LessThanOrEqualTo(shotgun.SpreadDegrees * .5f));
                    hasUnorderedPair |= angle < previousAngle;
                    previousAngle = angle;
                }

                Assert.That(hasUnorderedPair, Is.True, "Pellets must not occupy an ordered comb of angular slots.");
            }
            finally
            {
                UnityEngine.Random.state = previous;
            }
        }

        [Test]
        public void InvalidMagazineAndNonFiniteSavedTimersAreExplicitErrors()
        {
            JsonUtility.FromJsonOverwrite("{\"magazineCapacity\":0}", shotgun);
            Assert.Throws<InvalidOperationException>(() => shotgun.CreateCycle(5));
            JsonUtility.FromJsonOverwrite("{\"magazineCapacity\":4}", shotgun);
            var cycle = shotgun.CreateCycle(5);
            var state = cycle.Capture(0);
            state.cooldown = float.NaN;
            Assert.Throws<InvalidOperationException>(() => cycle.Restore(state, 0));
        }
    }
}
