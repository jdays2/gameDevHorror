using System;
using EmergencyShift.Features.Pickups.View;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class PickupGlintTests
    {
        [Test]
        public void DiscoveryRunsOnceAndHoldingTheBeamDoesNotLoop()
        {
            var pulse = new PickupGlintPulse(.6f, .65f);
            pulse.Step(true, .2f);
            Assert.That(pulse.Active, Is.True);
            Assert.That(pulse.Progress, Is.EqualTo(1f / 3).Within(.0001));
            pulse.Step(true, .5f);
            Assert.That(pulse.Active, Is.False);
            pulse.Step(true, 10);
            Assert.That(pulse.Active, Is.False);
        }

        [Test]
        public void LosingVisibilityHidesImmediatelyAndRequiresDarknessBeforeReentry()
        {
            var pulse = new PickupGlintPulse(.6f, .65f);
            pulse.Step(true, .1f);
            pulse.Step(false, .1f);
            Assert.That(pulse.Active, Is.False);
            pulse.Step(true, .1f);
            Assert.That(pulse.Active, Is.False);
            pulse.Step(false, .7f);
            pulse.Step(true, .1f);
            Assert.That(pulse.Active, Is.True);
        }

        [Test]
        public void PauseFreezesProgressAndDisablingResetsOnlyThisInstance()
        {
            var first = new PickupGlintPulse(.6f, .65f);
            var second = new PickupGlintPulse(.6f, .65f);
            first.Step(true, .2f);
            second.Step(true, .3f);
            float progress = first.Progress;
            first.Step(true, 0);
            Assert.That(first.Progress, Is.EqualTo(progress));
            first.Reset();
            Assert.That(first.Active, Is.False);
            Assert.That(second.Active, Is.True);
            Assert.That(second.Progress, Is.EqualTo(.5f));
        }

        [Test]
        public void InvalidTimingsAndNonfiniteDeltaAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupGlintPulse(float.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupGlintPulse(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupGlintPulse(1, -1));
            var pulse = new PickupGlintPulse(1, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => pulse.Step(true, float.PositiveInfinity));
        }

        [Test]
        public void AmmoPreviewUsesTargetInventoryEvenWhenAnotherWeaponIsActive()
        {
            var inventory = new WeaponInventory();
            var pistol = new WeaponSettings("pistol", 1, 1, 10, .2f, 5, false);
            var shotgun = new WeaponSettings("shotgun", 2, 1, 20, .2f, 5, false);
            Assert.That(inventory.CanAddAmmo("pistol"), Is.False);
            Assert.That(inventory.Acquire(pistol, 10), Is.True);
            Assert.That(inventory.Acquire(shotgun, 2), Is.True);
            Assert.That(inventory.CanAddAmmo("pistol"), Is.False);
            Assert.That(inventory.CanAddAmmo("shotgun"), Is.True);
            Assert.That(inventory.CanAddAmmo("unknown"), Is.False);
            Assert.That(inventory.AddAmmo(3, shotgun), Is.EqualTo(3));
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(5));
        }

        [Test]
        public void WeaponPreviewRejectsDuplicatesAndSlotConflictsWithoutGrantingAnything()
        {
            var inventory = new WeaponInventory();
            var pistol = new WeaponSettings("pistol", 1, 1, 10, .2f, 5, false);
            Assert.That(inventory.CanAcquire("pistol", 1), Is.True);
            Assert.That(inventory.Count, Is.Zero);
            Assert.That(inventory.Acquire(pistol, 2), Is.True);
            Assert.That(inventory.CanAcquire("pistol", 1), Is.False);
            Assert.That(inventory.CanAcquire("other-pistol", 1), Is.False);
            Assert.That(inventory.CanAcquire("shotgun", 2), Is.True);
            Assert.That(inventory.Count, Is.EqualTo(1));
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(2));
        }
    }
}
