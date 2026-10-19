using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Scheduling;
using EmergencyShift.Core.Time;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class WorkBudgetTests
    {
        [Test]
        public void QuotaResetsOnNextFrameWhileTotalsAndPeakRemain()
        {
            var clock = new TestFrameClock();
            var budget = new FrameWorkBudget(clock, 2);
            Assert.That(budget.TryReserve(), Is.True);
            Assert.That(budget.TryReserve(), Is.True);
            Assert.That(budget.TryReserve(), Is.False);
            clock.Frame++;
            Assert.That(budget.TryReserve(), Is.True);
            Assert.That(budget.TotalReservations, Is.EqualTo(3));
            Assert.That(budget.PeakReservations, Is.EqualTo(2));
        }

        [Test]
        public void NavigationAndFragmentsKeepTheirExistingIndependentQuotas()
        {
            var clock = new TestFrameClock();
            var paths = new NavigationWorkBudget2D(clock);
            var fragments = new GoreSpawnBudget2D(clock);
            for (int i = 0; i < NavigationWorkBudget2D.MaximumBuildsPerFrame; i++)
            {
                Assert.That(paths.TryReserve(), Is.True);
            }

            Assert.That(paths.TryReserve(), Is.False);
            for (int i = 0; i < GoreSpawnBudget2D.MaximumFragmentsPerFrame; i++)
            {
                Assert.That(fragments.TryReserve(), Is.True);
            }

            Assert.That(fragments.TryReserve(), Is.False);
            Assert.That(paths.PeakBuilds, Is.EqualTo(2));
            Assert.That(fragments.PeakFragments, Is.EqualTo(32));
            Assert.That(new NavigationWorkBudget2D(clock).TotalBuilds, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidQuotaIsRejectedBeforeUse(int maximum)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FrameWorkBudget(new TestFrameClock(), maximum));
        }

        [Test]
        public void OneActorsRandomDrawsCannotAdvanceAnotherActorsStream()
        {
            var seeds = new RandomSourceFactory(new Random(123));
            IRandomSource first = seeds.Create();
            IRandomSource second = seeds.Create();
            for (int i = 0; i < 100; i++)
            {
                first.NextUnit();
            }

            var expectedSeeds = new RandomSourceFactory(new Random(123));
            expectedSeeds.Create();
            IRandomSource expectedSecond = expectedSeeds.Create();
            Assert.That(second.NextUnit(), Is.EqualTo(expectedSecond.NextUnit()));
        }

        private sealed class TestFrameClock : IFrameClock
        {
            public int Frame { get; set; }
        }
    }
}
