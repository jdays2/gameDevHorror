using System;
using EmergencyShift.Features.Pickups;
using EmergencyShift.Features.Pickups.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class SingleUsePickupTests
    {
        [Test]
        public void ReentrantInventoryEventCannotApplyOnePickupTwice()
        {
            SingleUsePickupController controller = new PickupControllerFactory().CreateSingleUse();
            var state = new State();
            bool nested = true;
            state.OnApply = () => nested = controller.TryCollect(new Operation(state)).Success;
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(nested, Is.False);
            Assert.That(state.Grants, Is.EqualTo(1));
            Assert.That(state.Presentations, Is.EqualTo(1));
        }

        [Test]
        public void CommandRechecksActorAuthorization()
        {
            var model = new SingleUsePickupModel();
            var state = new State();
            var command = new CollectPickupCommand<Operation>(model, new Operation(state));
            Assert.That(command.CanExecute().Success, Is.True);
            state.Allowed = false;
            Assert.That(command.Execute().Success, Is.False);
            Assert.That(model.Collected, Is.False);
            Assert.That(state.Grants, Is.Zero);
        }

        [Test]
        public void RejectedEffectLeavesPickupAvailableForLater()
        {
            SingleUsePickupController controller = new PickupControllerFactory().CreateSingleUse();
            var state = new State { Accepted = false };
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.False);
            Assert.That(controller.Collected, Is.False);
            Assert.That(state.Presentations, Is.Zero);
            state.Accepted = true;
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(controller.Collected, Is.True);
        }

        [Test]
        public void ApplyFailureReleasesBusyGuard()
        {
            SingleUsePickupController controller = new PickupControllerFactory().CreateSingleUse();
            var state = new State { OnApply = () => throw new InvalidOperationException("before inventory grant") };
            Assert.Throws<InvalidOperationException>(() => controller.TryCollect(new Operation(state)));
            Assert.That(controller.Collected, Is.False);
            state.OnApply = null;
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
        }

        [Test]
        public void PresentationFailureCannotReopenAnAppliedPickup()
        {
            SingleUsePickupController controller = new PickupControllerFactory().CreateSingleUse();
            var state = new State { ThrowOnPresent = true };
            Assert.Throws<InvalidOperationException>(() => controller.TryCollect(new Operation(state)));
            Assert.That(controller.Collected, Is.True);
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.False);
            Assert.That(state.Grants, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreIsQuietAndDoesNotChangeInventory(bool consumed)
        {
            SingleUsePickupController controller = new PickupControllerFactory().CreateSingleUse();
            var state = new State();
            Assert.That(controller.Restore(consumed).Success, Is.True);
            Assert.That(controller.Collected, Is.EqualTo(consumed));
            Assert.That(state.Grants, Is.Zero);
            Assert.That(state.Presentations, Is.Zero);
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.EqualTo(!consumed));
        }

        [Test]
        public void RestoreCannotClearTheGuardInsideAnInventoryCallback()
        {
            SingleUsePickupController controller = new PickupControllerFactory().CreateSingleUse();
            var state = new State();
            bool restored = true;
            state.OnApply = () => restored = controller.Restore(false).Success;
            Assert.That(controller.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(restored, Is.False);
            Assert.That(controller.Collected, Is.True);
        }

        [Test]
        public void SeparateSceneInstancesCanApplyTheSameRepeatableEffect()
        {
            var factory = new PickupControllerFactory();
            SingleUsePickupController a = factory.CreateSingleUse();
            SingleUsePickupController b = factory.CreateSingleUse();
            var state = new State();
            Assert.That(a.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(a.TryCollect(new Operation(state)).Success, Is.False);
            Assert.That(b.TryCollect(new Operation(state)).Success, Is.True);
            Assert.That(state.Grants, Is.EqualTo(2));
        }

        [Test]
        public void ConsumedPickupAttemptsDoNotAllocateOrCallTheOperation()
        {
            SingleUsePickupController controller = new PickupControllerFactory().CreateSingleUse();
            var state = new State();
            controller.TryCollect(new Operation(state));
            controller.TryCollect(new Operation(state));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                controller.TryCollect(new Operation(state));
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(state.Grants, Is.EqualTo(1));
        }

        private sealed class State
        {
            public bool Allowed = true;
            public bool Accepted = true;
            public bool ThrowOnPresent;
            public int Grants;
            public int Presentations;
            public Action OnApply;
        }

        private readonly struct Operation : IPickupOperation
        {
            private readonly State state;
            public Operation(State state) => this.state = state;
            public bool CanApply => state.Allowed;
            public bool Apply()
            {
                state.OnApply?.Invoke();
                if (state.Accepted)
                {
                    state.Grants++;
                }

                return state.Accepted;
            }

            public void Present()
            {
                state.Presentations++;
                if (state.ThrowOnPresent)
                {
                    throw new InvalidOperationException("presentation");
                }
            }
        }
    }
}
