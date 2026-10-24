using System;
using EmergencyShift.Features.Pickups.View;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class PickupGlintTests
    {
        [Test]
        public void HeldBeamRepeatsAfterTheConfiguredPause()
        {
            var pulse = new PickupGlintPulse(1, .8f);
            pulse.Step(true, .25f);
            Assert.That(pulse.Active, Is.True);
            Assert.That(pulse.Progress, Is.EqualTo(.25f));
            pulse.Step(true, .75f);
            Assert.That(pulse.Active, Is.False);
            Assert.That(pulse.Progress, Is.EqualTo(1));
            pulse.Step(true, .7f);
            Assert.That(pulse.Active, Is.False);
            pulse.Step(true, .2f);
            Assert.That(pulse.Active, Is.True);
            Assert.That(pulse.Progress, Is.EqualTo(.1f).Within(.0001));
        }

        [Test]
        public void EveryReentryRestartsImmediatelyEvenDuringTheRepeatPause()
        {
            var pulse = new PickupGlintPulse(1, .8f);
            pulse.Step(true, .8f);
            pulse.Step(false, .001f);
            Assert.That(pulse.Active, Is.False);
            pulse.Step(true, .1f);
            Assert.That(pulse.Active, Is.True);
            Assert.That(pulse.Progress, Is.EqualTo(.1f).Within(.0001));
            pulse.Step(true, 1);
            Assert.That(pulse.Active, Is.False);
            pulse.Step(false, .001f);
            pulse.Step(true, .1f);
            Assert.That(pulse.Active, Is.True);
            Assert.That(pulse.Progress, Is.EqualTo(.1f).Within(.0001));
        }

        [Test]
        public void LongFramesKeepTheCycleBoundedAndZeroPauseIsSupported()
        {
            var pulse = new PickupGlintPulse(1, .5f);
            pulse.Step(true, 6.25f);
            Assert.That(pulse.Active, Is.True);
            Assert.That(pulse.Progress, Is.EqualTo(.25f));
            var continuous = new PickupGlintPulse(1, 0);
            continuous.Step(true, 3.25f);
            Assert.That(continuous.Active, Is.True);
            Assert.That(continuous.Progress, Is.EqualTo(.25f));
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
            Assert.That(second.Progress, Is.EqualTo(.5f).Within(.0001));
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
