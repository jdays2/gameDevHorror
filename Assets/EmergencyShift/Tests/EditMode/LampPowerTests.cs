using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.LampPower;
using EmergencyShift.Features.LampPower.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class LampPowerTests
    {
        [Test]
        public void PowerCommandCommitsBeforeApplyingViewAndRepeatedPowerRefreshes()
        {
            var model = new LampPowerModel(new Clock(), false);
            var view = new View();
            var controller = new LampPowerController(model, view);
            view.OnPower = () => Assert.That(model.Powered, Is.EqualTo(view.Powered));
            controller.SetPower(true);
            controller.SetPower(true);
            Assert.That(view.Applies, Is.EqualTo(2));
            controller.SetPower(false);
            Assert.That(model.Powered, Is.False);
            Assert.That(view.Powered, Is.False);
        }

        [Test]
        public void OffLampDoesNotReadClockOrApplyFlicker()
        {
            var clock = new Clock { Now = double.NaN };
            var view = new View();
            var controller = new LampPowerController(new LampPowerModel(clock, false), view);
            controller.Tick(float.NaN, new Vector2(float.NaN, 0));
            Assert.That(clock.Reads, Is.Zero);
            Assert.That(view.Flickers, Is.Zero);
        }

        [Test]
        public void ClockUsesLegacyFloatOrderAndAuthoredPositionPhase()
        {
            var clock = new Clock { Now = 401.12345 };
            var view = new View();
            var controller = new LampPowerController(new LampPowerModel(clock, true), view);
            var position = new Vector2(-31.56f, 12.345f);
            controller.Tick(-2.34f, position);
            Assert.That(view.Clock, Is.EqualTo((float)clock.Now - 2.34f + position.x * .37f + position.y * .19f));
            Assert.That(clock.Reads, Is.EqualTo(1));
            controller.Tick(-2.34f, position);
            Assert.That(view.Clock, Is.EqualTo((float)clock.Now - 2.34f + position.x * .37f + position.y * .19f));
        }

        [TestCase(float.NaN, 0, 0)]
        [TestCase(0, float.PositiveInfinity, 0)]
        [TestCase(0, 0, float.NegativeInfinity)]
        public void InvalidAuthoredPhaseRejectsBeforeReadingClock(float phase, float x, float y)
        {
            var clock = new Clock();
            var model = new LampPowerModel(clock, true);
            Assert.Throws<ArgumentOutOfRangeException>(() => model.SampleClock(phase, new Vector2(x, y)));
            Assert.That(clock.Reads, Is.Zero);
            Assert.That(model.Powered, Is.True);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.MaxValue)]
        public void InvalidClockDoesNotReachView(double now)
        {
            var view = new View();
            var controller = new LampPowerController(new LampPowerModel(new Clock { Now = now }, true), view);
            Assert.Throws<InvalidOperationException>(() => controller.Tick(0, Vector2.zero));
            Assert.That(view.Flickers, Is.Zero);
            Assert.That(controller.Powered, Is.True);
        }

        [Test]
        public void FiniteInputsCannotOverflowCombinedClock()
        {
            var model = new LampPowerModel(new Clock { Now = float.MaxValue }, true);
            Assert.Throws<InvalidOperationException>(() => model.SampleClock(float.MaxValue, Vector2.zero));
        }

        [Test]
        public void ScopedFactoryCreatesIndependentPowerAndRefreshDoesNotAdvanceClock()
        {
            var clock = new Clock();
            var factory = new LampPowerControllerFactory(clock);
            var first = factory.Create(new View(), false);
            var second = factory.Create(new View(), true);
            first.SetPower(true);
            second.SetPower(false);
            first.Refresh();
            Assert.That(first.Powered, Is.True);
            Assert.That(second.Powered, Is.False);
            Assert.That(clock.Reads, Is.Zero);
            Assert.That(new SetLampPowerCommand(null, true).Execute().Success, Is.False);
        }

        [Test]
        public void WarmedClockAndPowerLoopDoesNotAllocateManagedObjects()
        {
            var clock = new Clock();
            var controller = new LampPowerControllerFactory(clock).Create(new View(), true);
            for (int index = 0; index < 100; index++)
            {
                controller.Tick(0, Vector2.zero);
                controller.SetPower(true);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1000; index++)
            {
                clock.Now = index;
                controller.Tick(0, Vector2.zero);
                controller.SetPower(true);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        private sealed class Clock : IGameClock
        {
            public double Now;
            public int Reads;
            public double Time
            {
                get
                {
                    Reads++;
                    return Now;
                }
            }
        }

        private sealed class View : ILampPowerView
        {
            public int Applies;
            public int Flickers;
            public bool Powered;
            public float Clock;
            public Action OnPower;

            public void ApplyPower(bool powered)
            {
                Powered = powered;
                Applies++;
                OnPower?.Invoke();
            }

            public void ApplyFlicker(float clock)
            {
                Clock = clock;
                Flickers++;
            }
        }
    }
}
