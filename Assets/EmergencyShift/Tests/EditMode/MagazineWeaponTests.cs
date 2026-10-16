using System;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class MagazineWeaponTests
    {
        private MagazineWeaponDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<MagazineWeaponDefinition>();
            definition.magazineCapacity = 12;
            definition.secondsBetweenShots = .1f;
            definition.maximumAmmo = 120;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }

        private WeaponCycle EmptyMagazine()
        {
            var cycle = definition.CreateCycle(30);
            for (int index = 0; index < 12; index++)
            {
                Assert.That(cycle.TryFire(index * .11f), Is.True);
            }

            return cycle;
        }

        [Test]
        public void ReserveCannotFireUntilAtomicReloadCompletes()
        {
            var cycle = EmptyMagazine();
            Assert.That(cycle.TotalAmmo, Is.EqualTo(18));
            Assert.That(cycle.Loaded, Is.Zero);
            Assert.That(cycle.TryFire(2), Is.False);
            Assert.That(cycle.CanDryFire, Is.True);
            Assert.That(cycle.TryReload(2), Is.True);
            Assert.That(cycle.CanDryFire, Is.False);
            Assert.That(cycle.TryFire(2.5f), Is.False);
            cycle.Tick(3.39f);
            Assert.That(cycle.Loaded, Is.Zero);
            cycle.Tick(3.41f);
            Assert.That(cycle.Loaded, Is.EqualTo(12));
            Assert.That(cycle.Reserve, Is.EqualTo(6));
            Assert.That(cycle.TotalAmmo, Is.EqualTo(18));
            cycle.Tick(10);
            Assert.That(cycle.TotalAmmo, Is.EqualTo(18));
        }

        [Test]
        public void CancelledPartialReloadKeepsBothAmmoCounts()
        {
            var cycle = definition.CreateCycle(30);
            cycle.TryFire(0);
            cycle.TryReload(1);
            cycle.CancelReload();
            cycle.Tick(10);
            Assert.That(cycle.Loaded, Is.EqualTo(11));
            Assert.That(cycle.Reserve, Is.EqualTo(18));
            Assert.That(cycle.TotalAmmo, Is.EqualTo(29));
        }

        [Test]
        public void PendingReloadRoundTripCommitsOnceWithoutRestartCue()
        {
            var cycle = EmptyMagazine();
            cycle.TryReload(2);
            var state = JsonUtility.FromJson<WeaponCycleSnapshot>(JsonUtility.ToJson(cycle.Capture(2.5f)));
            var restored = definition.CreateCycle(0);
            int committed = 0;
            int started = 0;
            restored.Operation += operation =>
            {
                if (operation == WeaponOperation.MagazineLoaded)
                {
                    committed++;
                }
                if (operation == WeaponOperation.MagazineReloadStarted)
                {
                    started++;
                }
            };
            restored.Restore(state, 100);
            restored.Tick(100.8f);
            Assert.That(restored.Loaded, Is.Zero);
            restored.Tick(101);
            restored.Tick(110);
            Assert.That(restored.Loaded, Is.EqualTo(12));
            Assert.That(restored.TotalAmmo, Is.EqualTo(18));
            Assert.That(committed, Is.EqualTo(1));
            Assert.That(started, Is.Zero);
        }

        [Test]
        public void LegacyUnlimitedPistolMigratesWithoutCreatingCartridges()
        {
            var old = new WeaponCycle(definition, 29).Capture(0);
            var restored = definition.CreateCycle(0);
            restored.Restore(old, 10);
            Assert.That(restored.Loaded, Is.EqualTo(12));
            Assert.That(restored.Reserve, Is.EqualTo(17));
            Assert.That(restored.Capture(10).format, Is.EqualTo(2));
            old.phase = WeaponPhase.Loading;
            Assert.Throws<InvalidOperationException>(() => restored.Restore(old, 10));
        }

        [Test]
        public void SharedDefinitionDoesNotShareMagazineOrReloadState()
        {
            var first = definition.CreateCycle(30);
            var second = definition.CreateCycle(30);
            first.TryFire(0);
            first.TryReload(1);
            Assert.That(second.Loaded, Is.EqualTo(12));
            Assert.That(second.Phase, Is.EqualTo(WeaponPhase.Ready));
            Assert.That(definition.magazineCapacity, Is.EqualTo(12));
        }

        [Test]
        public void FullMagazineEmptyReserveAndMalformedSnapshotsAreRejected()
        {
            var cycle = definition.CreateCycle(12);
            Assert.That(cycle.TryReload(0), Is.False);
            cycle.TryFire(0);
            Assert.That(cycle.TryReload(1), Is.False);
            var saved = cycle.Capture(1);
            saved.loaded = 12;
            Assert.Throws<InvalidOperationException>(() => cycle.Restore(saved, 2));
            saved.loaded = 11;
            saved.operationRemaining = float.NaN;
            Assert.Throws<InvalidOperationException>(() => cycle.Restore(saved, 2));
        }
    }
}
