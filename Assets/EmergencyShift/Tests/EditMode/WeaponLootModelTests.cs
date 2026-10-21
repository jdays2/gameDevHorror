using System;
using EmergencyShift.Features.Pickups;
using EmergencyShift.Features.Pickups.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class WeaponLootModelTests
    {
        [Test]
        public void PartialAmmoLeavesRemainderAndDepletesOnlyAfterActualAcceptance()
        {
            WeaponLootController controller = Create(true);
            var state = new State { Received = 2 };
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(controller.Remaining, Is.EqualTo(6));
            Assert.That(controller.Collected, Is.False);
            state.Received = 0;
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.False);
            Assert.That(controller.Remaining, Is.EqualTo(6));
            state.Received = 6;
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(controller.Remaining, Is.Zero);
            Assert.That(controller.Collected, Is.True);
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.False);
        }

        [Test]
        public void WeaponAcquisitionConsumesOnceAndRetainsItsLegacyAmmoField()
        {
            WeaponLootController controller = Create(false);
            var state = new State();
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(controller.Collected, Is.True);
            Assert.That(controller.Remaining, Is.EqualTo(8));
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.False);
            Assert.That(state.Effects, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReentryRestoreAndConfigureAreBlockedDuringEffectAndFeedback(bool duringFeedback)
        {
            WeaponLootController controller = Create(true);
            var state = new State { Received = 2 };
            bool nested = true;
            bool restored = true;
            bool configured = true;
            Action callback = () =>
            {
                nested = controller.TryCollect(new Operation(state)).Success;
                restored = controller.Restore(false, 8).Success;
                configured = controller.Configure(false, 8).Success;
            };
            if (duringFeedback)
            {
                state.OnPresent = callback;
            }
            else
            {
                state.OnApply = callback;
            }

            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(nested || restored || configured, Is.False);
            Assert.That(controller.Remaining, Is.EqualTo(6));
            Assert.That(state.Effects, Is.EqualTo(1));
        }

        [Test]
        public void CommandRechecksActorBeforeInventoryMutation()
        {
            WeaponLootController controller = Create(true);
            var state = new State();
            var command = new CollectWeaponLootCommand<Operation>(controller, new Operation(state));
            Assert.That(command.CanExecute().Success, Is.True);
            state.Allowed = false;
            Assert.That(command.Execute().Success, Is.False);
            Assert.That(state.Effects, Is.Zero);
            Assert.That(controller.Remaining, Is.EqualTo(8));
        }

        [TestCase(false, 0)]
        [TestCase(true, 5)]
        public void RestoreIsQuietAndKeepsExplicitState(bool consumed, int remaining)
        {
            WeaponLootController controller = Create(true);
            Assert.That(controller.Restore(consumed, remaining).Success, Is.True);
            Assert.That(controller.Remaining, Is.EqualTo(remaining));
            Assert.That(controller.Collected, Is.EqualTo(consumed));
            Assert.That(controller.Restore(false, -1).Success, Is.False);
            Assert.That(controller.Remaining, Is.EqualTo(remaining));
        }

        [Test]
        public void ConfigureChangesModeAndAmountWithoutReopeningConsumedLoot()
        {
            WeaponLootController controller = Create(true);
            controller.Restore(true, 0);
            Assert.That(controller.Configure(false, 5).Success, Is.True);
            Assert.That(controller.Remaining, Is.EqualTo(5));
            Assert.That(controller.Collected, Is.True);
            Assert.That(controller.Configure(true, -1).Success, Is.False);
        }

        [Test]
        public void ApplyFailureReleasesGuardWithoutChangingBox()
        {
            WeaponLootController controller = Create(true);
            var state = new State { OnApply = () => throw new InvalidOperationException("before grant") };
            Assert.Throws<InvalidOperationException>(() => controller.TryCollect(new Operation(state)));
            Assert.That(controller.CanCollect, Is.True);
            Assert.That(controller.Remaining, Is.EqualTo(8));
        }

        [Test]
        public void FeedbackFailureKeepsCommittedRemainderAndReleasesGuard()
        {
            WeaponLootController controller = Create(true);
            var state = new State { Received = 2, OnPresent = () => throw new InvalidOperationException("feedback") };
            Assert.Throws<InvalidOperationException>(() => controller.TryCollect(new Operation(state)));
            Assert.That(controller.CanCollect, Is.True);
            Assert.That(controller.Remaining, Is.EqualTo(6));
        }

        [Test]
        public void SharedFactoryDoesNotShareRemainderOrConsumption()
        {
            var factory = new PickupControllerFactory();
            WeaponLootController a = factory.CreateWeaponLoot(true, 8);
            WeaponLootController b = factory.CreateWeaponLoot(true, 8);
            a.TryCollect(new Operation(new State { Received = 8 }));
            Assert.That(a.Collected, Is.True);
            Assert.That(b.Collected, Is.False);
            Assert.That(b.Remaining, Is.EqualTo(8));
        }

        [Test]
        public void NegativeAuthoredAmountIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(true, -1));
        }

        private static WeaponLootController Create(bool isAmmo, int amount = 8) => new PickupControllerFactory().CreateWeaponLoot(isAmmo, amount);

        private sealed class State
        {
            public bool Allowed = true;
            public int Received = 1;
            public int Effects;
            public Action OnApply;
            public Action OnPresent;
        }

        private readonly struct Operation : IWeaponLootOperation
        {
            private readonly State state;
            public Operation(State state) => this.state = state;
            public bool CanApply => state.Allowed;
            public WeaponLootGrant Apply(bool isAmmo, int cartridges)
            {
                state.OnApply?.Invoke();
                state.Effects++;
                return isAmmo ? WeaponLootGrant.Ammo(state.Received) : WeaponLootGrant.Weapon(true);
            }

            public void Present(bool isAmmo, int received, bool consumed) => state.OnPresent?.Invoke();
        }
    }
}
