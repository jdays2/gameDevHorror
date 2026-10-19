using EmergencyShift.Core.Time;
using EmergencyShift.Features.Health.Config;
using EmergencyShift.Features.Health.Logic;
using NUnit.Framework;

namespace EmergencyShift.Features.Health.Tests
{
    public sealed class HealthModelTests
    {
        [Test]
        public void Damage_OverkillAndRepeatedHit_EmitsOneDeath()
        {
            var clock = new ManualClock();
            var model = new HealthModel(new HealthSettings(2, 0), clock);
            var observer = new Observer();
            model.Died += observer.OnDied;
            model.Damaged += observer.OnDamaged;
            try
            {
                Assert.That(new DamageHealthCommand(model, 99).Execute().Success, Is.True);
                Assert.That(new DamageHealthCommand(model, 1).Execute().Success, Is.False);
                Assert.That(model.Current, Is.Zero);
                Assert.That(observer.Deaths, Is.EqualTo(1));
                Assert.That(observer.Hits, Is.Zero);
            }
            finally
            {
                model.Died -= observer.OnDied;
                model.Damaged -= observer.OnDamaged;
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Damage_InvalidAmount_DoesNotChangeState(int amount)
        {
            var model = new HealthModel(new HealthSettings(3, 0), new ManualClock());
            Assert.That(new DamageHealthCommand(model, amount).CanExecute().Success, Is.False);
            Assert.That(new DamageHealthCommand(model, amount).Execute().Success, Is.False);
            Assert.That(model.Current, Is.EqualTo(3));
        }

        [Test]
        public void Damage_ProtectionUsesInjectedClock_ExpiresAtBoundary()
        {
            var clock = new ManualClock();
            var model = new HealthModel(new HealthSettings(3, 0.7), clock);
            Assert.That(new DamageHealthCommand(model, 1).Execute().Success, Is.True);
            clock.Time = 0.69;
            Assert.That(new DamageHealthCommand(model, 1).Execute().Success, Is.False);
            clock.Time = 0.7;
            Assert.That(new DamageHealthCommand(model, 1).Execute().Success, Is.True);
            Assert.That(model.Current, Is.EqualTo(1));
        }

        [Test]
        public void Damage_TwoActorsWithSharedSettings_KeepIndependentHealthAndProtection()
        {
            var settings = new HealthSettings(3, 1);
            var clock = new ManualClock();
            var first = new HealthModel(settings, clock);
            var second = new HealthModel(settings, clock);
            new DamageHealthCommand(first, 1).Execute();
            Assert.That(first.Current, Is.EqualTo(2));
            Assert.That(second.Current, Is.EqualTo(3));
            Assert.That(new DamageHealthCommand(second, 1).Execute().Success, Is.True);
            Assert.That(settings.Maximum, Is.EqualTo(3));
        }

        [Test]
        public void Restore_DeadSnapshot_IsSilentAndDoesNotPermitAnotherDeath()
        {
            var model = new HealthModel(new HealthSettings(2, 0), new ManualClock());
            var observer = new Observer();
            model.Died += observer.OnDied;
            try
            {
                model.Restore(0);
                Assert.That(observer.Deaths, Is.Zero);
                Assert.That(new DamageHealthCommand(model, 1).Execute().Success, Is.False);
                Assert.That(observer.Deaths, Is.Zero);
            }
            finally
            {
                model.Died -= observer.OnDied;
            }
        }

        [Test]
        public void Restore_LivingSnapshot_ResetsProtectionWithoutDamageEvent()
        {
            var model = new HealthModel(new HealthSettings(3, 10), new ManualClock());
            new DamageHealthCommand(model, 1).Execute();
            model.Restore(3);
            Assert.That(new DamageHealthCommand(model, 1).Execute().Success, Is.True);
            Assert.That(model.Current, Is.EqualTo(2));
        }

        [Test]
        public void Capacity_DeadActor_CannotBeRevivedBySandboxRule()
        {
            var model = new HealthModel(new HealthSettings(2, 0), new ManualClock());
            new DamageHealthCommand(model, 2).Execute();
            Assert.That(new SetHealthCapacityCommand(model, 100, 100).Execute().Success, Is.False);
            Assert.That(model.Current, Is.Zero);
            Assert.That(model.Maximum, Is.EqualTo(2));
        }

        [Test]
        public void Capacity_LivingActor_ChangesOnlyInstanceAndClampsCurrent()
        {
            var settings = new HealthSettings(2, 0);
            var model = new HealthModel(settings, new ManualClock());
            Assert.That(new SetHealthCapacityCommand(model, 100, 200).Execute().Success, Is.True);
            Assert.That(model.Maximum, Is.EqualTo(100));
            Assert.That(model.Current, Is.EqualTo(100));
            Assert.That(settings.Maximum, Is.EqualTo(2));
        }

        private sealed class ManualClock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Observer
        {
            public int Deaths { get; private set; }
            public int Hits { get; private set; }

            public void OnDied() => Deaths++;
            public void OnDamaged() => Hits++;
        }
    }
}
