using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Doors.Config;
using EmergencyShift.Features.Doors.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class DoorMotionTests
    {
        [Test]
        public void LockedDoorRejectsAndOpeningDoorCannotBeRetoggled()
        {
            var model = Model(new Clock());
            Assert.That(new ToggleDoorCommand(model, false).Execute().Success, Is.False);
            Assert.That(model.IsOpen, Is.False);
            var waitingCommand = new ToggleDoorCommand(model, true);
            Assert.That(waitingCommand.CanExecute().Success, Is.True);
            Assert.That(new ToggleDoorCommand(model, true).Execute().Success, Is.True);
            Assert.That(waitingCommand.Execute().Success, Is.False, "Execute must revalidate after another command.");
            Assert.That(model.IsOpen, Is.True);
            Assert.That(model.IsMoving, Is.True);
        }

        [Test]
        public void ScaledClockDrivesSmoothOpeningAndClosingWithOneStartStopPair()
        {
            var clock = new Clock();
            var model = Model(clock);
            var view = new View();
            using var controller = new DoorMotionController(model, view);
            controller.Toggle(true);
            clock.Time = .25;
            controller.Tick();
            Assert.That(view.Amount, Is.EqualTo(.15625f).Within(.0001));
            controller.Tick();
            Assert.That(view.Amount, Is.EqualTo(.15625f).Within(.0001), "Paused clock must freeze the leaf.");
            clock.Time = 1;
            controller.Tick();
            controller.Tick();
            Assert.That(view.Amount, Is.EqualTo(1));
            Assert.That(model.IsMoving, Is.False);
            Assert.That(view.Starts, Is.EqualTo(1));
            Assert.That(view.Stops, Is.EqualTo(1));
            Assert.That(controller.Toggle(false).Success, Is.True, "An open door can close without a card.");
            clock.Time = 1.5;
            controller.Tick();
            Assert.That(view.Amount, Is.EqualTo(.5f).Within(.0001));
            clock.Time = 2;
            controller.Tick();
            Assert.That(view.Amount, Is.Zero);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(view.Starts, Is.EqualTo(2));
            Assert.That(view.Stops, Is.EqualTo(2));
        }

        [Test]
        public void ConfirmationWaitKeepsDoorClosedAndStartsOnlyAfterDeadline()
        {
            var clock = new Clock();
            var model = Model(clock);
            var view = new View();
            using var controller = new DoorMotionController(model, view);
            controller.Toggle(true, .5f);
            Assert.That(model.IsAwaitingAccess, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CanInteract, Is.False);
            clock.Time = .49;
            controller.Tick();
            Assert.That(view.Starts, Is.Zero);
            clock.Time = .5;
            controller.Tick();
            Assert.That(model.IsOpen, Is.True);
            Assert.That(view.Starts, Is.EqualTo(1));
            Assert.That(view.Amount, Is.Zero);
            clock.Time = 1.5;
            controller.Tick();
            Assert.That(view.Amount, Is.EqualTo(1));
        }

        [Test]
        public void DisableCancelsPendingConfirmationWithoutOpeningOrAudioEvents()
        {
            var clock = new Clock();
            var model = Model(clock);
            var view = new View();
            using var controller = new DoorMotionController(model, view);
            controller.Toggle(true, .5f);
            controller.Stop();
            clock.Time = 5;
            controller.Tick();
            Assert.That(model.CanInteract, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(view.Amount, Is.Zero);
            Assert.That(view.Starts + view.Stops, Is.Zero);
        }

        [Test]
        public void InterruptedMotionSettlesToRequestedEndpointAndStopsOnlyOnce()
        {
            var model = Model(new Clock());
            var view = new View();
            using var controller = new DoorMotionController(model, view);
            controller.Toggle(true);
            controller.Stop();
            controller.Stop();
            Assert.That(view.Amount, Is.EqualTo(1));
            Assert.That(view.Stops, Is.EqualTo(1));
            Assert.That(controller.CanInteract, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreIsSilentCancelsAllWorkAndCannotResumeOldTimeline(bool open)
        {
            var clock = new Clock();
            var model = Model(clock);
            var view = new View();
            using var controller = new DoorMotionController(model, view);
            controller.Toggle(true, .5f);
            controller.Restore(open);
            clock.Time = 10;
            controller.Tick();
            Assert.That(view.Starts + view.Stops, Is.Zero);
            Assert.That(controller.CanInteract, Is.True);
            Assert.That(controller.IsOpen, Is.EqualTo(open));
            Assert.That(view.Amount, Is.EqualTo(open ? 1 : 0));
        }

        [Test]
        public void SharedClockDoesNotShareDoorState()
        {
            var clock = new Clock();
            var first = Model(clock);
            var second = Model(clock);
            new ToggleDoorCommand(first, true).Execute();
            clock.Time = 1;
            first.Tick();
            second.Tick();
            Assert.That(first.IsOpen, Is.True);
            Assert.That(second.IsOpen, Is.False);
            Assert.That(second.CanInteract, Is.True);
        }

        [Test]
        public void DisposalUnsubscribesAndPreventsFurtherControllerWrites()
        {
            var clock = new Clock();
            var model = Model(clock);
            var view = new View();
            var controller = new DoorMotionController(model, view);
            controller.Dispose();
            controller.Dispose();
            new ToggleDoorCommand(model, true).Execute();
            clock.Time = 2;
            model.Tick();
            Assert.That(view.Starts + view.Stops, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => controller.Tick());
            Assert.Throws<ObjectDisposedException>(() => controller.Toggle(true));
            Assert.Throws<ObjectDisposedException>(() => controller.Restore(false));
        }

        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidConfirmationCannotChangeState(float delay)
        {
            var model = Model(new Clock());
            Assert.That(new ToggleDoorCommand(model, true, delay).Execute().Success, Is.False);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CanInteract, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDurationIsRejectedAtComposition(float duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DoorMotionSettings(duration));
        }

        private static DoorMotionModel Model(Clock clock) => new DoorMotionModel(new DoorMotionSettings(1), clock);

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class View : IDoorMotionView
        {
            public float Amount;
            public int Starts;
            public int Stops;
            public void ApplyOpenAmount(float amount) => Amount = amount;
            public void MotionStarted(bool open) => Starts++;
            public void MotionStopped(bool open) => Stops++;
        }
    }
}
