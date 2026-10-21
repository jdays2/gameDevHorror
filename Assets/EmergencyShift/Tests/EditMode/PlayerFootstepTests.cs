using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.PlayerFootsteps.Config;
using EmergencyShift.Features.PlayerFootsteps.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class PlayerFootstepTests
    {
        [Test]
        public void RenderFramesBetweenPhysicsTicksPreservePartialStride()
        {
            var model = Model(new Clock());
            Reset(model);
            Assert.That(model.Tick(Vector2.right * .6f, 1, true), Is.False);
            Assert.That(model.Tick(Vector2.right * .6f, 1, true), Is.False);
            Assert.That(model.Tick(Vector2.right * 1.1f, 1, true), Is.True);
        }

        [Test]
        public void StandingAtWallClearsStrideEvenWhenInputCouldStillBeHeld()
        {
            var model = Model(new Clock());
            Reset(model);
            model.Tick(Vector2.right * .6f, 1, true);
            Assert.That(model.Tick(Vector2.right * .6f, 0, true), Is.False);
            Assert.That(model.Tick(Vector2.right * 1.1f, 1, true), Is.False);
        }

        [Test]
        public void TeleportDoesNotSoundOrCarryItsDistanceIntoNextStride()
        {
            var model = Model(new Clock());
            Reset(model);
            Assert.That(model.Tick(Vector2.right * 10, 1, true), Is.False);
            Assert.That(model.Tick(Vector2.right * 10.6f, 1, true), Is.False);
            Assert.That(model.Tick(Vector2.right * 11.2f, 1, true), Is.True);
        }

        [Test]
        public void DisabledDeadOrPausedTravelClearsStrideAndTracksPosition()
        {
            var model = Model(new Clock());
            Reset(model);
            model.Tick(Vector2.right * .6f, 1, true);
            Assert.That(model.Tick(Vector2.right * 1.2f, 1, false), Is.False);
            Assert.That(model.Tick(Vector2.right * 1.8f, 1, true), Is.False);
        }

        [Test]
        public void MinimumIntervalDefersContactAndKeepsRemainderWithoutBurst()
        {
            var clock = new Clock();
            var model = Model(clock);
            Reset(model);
            Assert.That(model.Tick(Vector2.right * 1.1f, 1, true), Is.True);
            clock.Time = .1;
            Assert.That(model.Tick(Vector2.right * 2.1f, 1, true), Is.False);
            clock.Time = .6;
            Assert.That(model.Tick(Vector2.right * 2.2f, 1, true), Is.True);
            Assert.That(model.Tick(Vector2.right * 2.2f, 1, true), Is.False);
            clock.Time = 2;
            Assert.That(model.Tick(Vector2.right * 2.8f, 1, true), Is.False);
        }

        [Test]
        public void ResetClearsBothStrideAndCooldown()
        {
            var model = Model(new Clock());
            Reset(model);
            Assert.That(model.Tick(Vector2.right * 1.1f, 1, true), Is.True);
            Reset(model);
            Assert.That(model.Tick(Vector2.right * 1.1f, 1, true), Is.True);
        }

        [Test]
        public void PersonalModelsAndViewsDoNotShareStride()
        {
            var clock = new Clock();
            var first = Model(clock);
            var second = Model(clock);
            var firstView = new View();
            var secondView = new View();
            var a = new PlayerFootstepController(first, firstView);
            var b = new PlayerFootstepController(second, secondView);
            a.Reset(Vector2.zero);
            b.Reset(Vector2.zero);
            a.Sample(Vector2.right * 1.1f, 1, true);
            b.Sample(Vector2.right * .2f, 1, true);
            Assert.That(firstView.Contacts, Is.EqualTo(1));
            Assert.That(secondView.Contacts, Is.Zero);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.MaxValue)]
        public void InvalidResetIsAtomicAndInvalidSampleRecoversWithoutPhantomContact(float value)
        {
            var model = Model(new Clock());
            Reset(model);
            model.Tick(Vector2.right * .6f, 1, true);
            Assert.That(new ResetFootstepCommand(model, new Vector2(value, 0)).Execute().Success, Is.False);
            Assert.That(model.Tick(Vector2.right * 1.1f, 1, true), Is.True);
            Assert.That(model.Tick(new Vector2(value, 0), 1, true), Is.False);
            Assert.That(model.Tick(Vector2.right * 20, 1, true), Is.False);
            Assert.That(model.Tick(Vector2.right * 20.2f, 1, true), Is.False);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1)]
        public void InvalidClockCannotEmitContact(double value)
        {
            var clock = new Clock { Time = value };
            var model = Model(clock);
            Reset(model);
            Assert.That(model.Tick(Vector2.right * 1.1f, 1, true), Is.False);
        }

        [TestCase(0, .2f)]
        [TestCase(float.NaN, .2f)]
        [TestCase(float.PositiveInfinity, .2f)]
        [TestCase(1, -1)]
        [TestCase(1, float.NaN)]
        public void InvalidCadenceIsRejected(float distance, float interval)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerFootstepSettings(distance, interval));
        }

        [Test]
        public void ContinuousSamplingDoesNotAllocateManagedObjects()
        {
            var clock = new Clock();
            var view = new View();
            var controller = new PlayerFootstepController(Model(clock), view);
            for (int i = 0; i < 50; i++)
            {
                clock.Time = i;
                controller.Sample(Vector2.right * i * .1f, 1, true);
            }

            controller.Reset(Vector2.zero);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                clock.Time = i;
                controller.Sample(Vector2.right * i * .1f, 1, true);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(view.Contacts, Is.GreaterThan(0));
            Assert.That(allocated, Is.Zero);
        }

        private static PlayerFootstepModel Model(Clock clock) => new PlayerFootstepModel(new PlayerFootstepSettings(1, .5f), clock);
        private static void Reset(PlayerFootstepModel model) => new ResetFootstepCommand(model, Vector2.zero).Execute();

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class View : IPlayerFootstepView
        {
            public int Contacts;
            public void PlayStep(Vector2 position) => Contacts++;
        }
    }
}
