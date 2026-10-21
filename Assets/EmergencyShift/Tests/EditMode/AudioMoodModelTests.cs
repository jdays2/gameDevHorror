using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood.Config;
using EmergencyShift.Features.AudioMood.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class AudioMoodModelTests
    {
        private Clock clock;
        private AudioMoodModel model;
        private View view;
        private AudioMoodController controller;
        private static readonly AudioThreatObservation[] Empty = Array.Empty<AudioThreatObservation>();

        [SetUp]
        public void Setup()
        {
            clock = new Clock { Time = 10 };
            model = new AudioMoodModel(Settings(), clock);
            view = new View();
            controller = new AudioMoodController(model, view);
        }

        [Test]
        public void SilentUnseenAggroDoesNotRevealTheEnemy()
        {
            Observe(Source(engaged: true));
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
            Assert.That(view.Perceived, Is.Zero);
        }

        [Test]
        public void SeenUnengagedEnemyStartsThreat()
        {
            Observe(Source(seen: true));
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Threat));
            Assert.That(view.Perceived, Is.EqualTo(1));
            Assert.That(view.Engaged, Is.Zero);
        }

        [Test]
        public void HeardDeadlineIsInclusiveAndWorksWithoutSight()
        {
            Observe(Source(heardUntil: 10));
            Assert.That(view.Perceived, Is.EqualTo(1));
        }

        [Test]
        public void MemoryDeadlineIsInclusiveAndThenExpires()
        {
            Observe(Source(seen: true));
            clock.Time = 11.25;
            Observe(Source());
            Assert.That(view.Perceived, Is.EqualTo(1));
            clock.Time = 11.26;
            Observe(Source());
            Assert.That(view.Perceived, Is.Zero);
            Assert.That(model.RememberedCount, Is.Zero);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Threat));
            clock.Time = 16.25;
            Observe(Source());
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeadOrUnregisteredSourceIsRemovedBeforeMemoryExpiry(bool dead)
        {
            Observe(Source(seen: true));
            controller.Observe(dead ? new[] { Source(alive: false) } : Empty, true);
            Assert.That(model.RememberedCount, Is.Zero);
            Assert.That(view.Perceived, Is.Zero);
        }

        [Test]
        public void IneligibleSourceCannotRevealItselfThroughSoundOrAttack()
        {
            Observe(Source(eligible: false, seen: true, heardUntil: 20, urgentUntil: 20, engaged: true, attacking: true));
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
            Assert.That(controller.IsDucking, Is.False);
        }

        [TestCase(82)]
        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidOrDistantObservationIsNotPerceived(float distance)
        {
            Observe(Source(seen: true, distanceSquared: distance, engaged: true, attacking: true));
            Assert.That(view.Perceived, Is.Zero);
        }

        [Test]
        public void ClosePursuerAtBoundaryStartsCombatWithoutShooting()
        {
            Observe(Source(seen: true, engaged: true, distanceSquared: 6.25f));
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Combat));
        }

        [Test]
        public void TwoDistantPursuersStartCombatButOneDoesNot()
        {
            Observe(Source(seen: true, engaged: true));
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Threat));
            controller.Observe(new[] { Source(seen: true, engaged: true), Source(id: 2, seen: true, engaged: true) }, true);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Combat));
            Assert.That(view.Engaged, Is.EqualTo(2));
        }

        [Test]
        public void PerceivedAttackStartsCombat()
        {
            Observe(Source(heardUntil: 11, attacking: true));
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Combat));
        }

        [Test]
        public void EmptyShotDucksWithoutStartingCombat()
        {
            Assert.That(controller.Shot().Success, Is.True);
            controller.Observe(Empty, true);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
            Assert.That(controller.IsDucking, Is.True);
            clock.Time = 10.23;
            Assert.That(controller.IsDucking, Is.False);
        }

        [Test]
        public void LethalProjectileBeforeNextObservationStillHoldsCombat()
        {
            Observe(Source(seen: true, engaged: true));
            controller.Shot();
            controller.Observe(Empty, true);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Combat));
            clock.Time = 13;
            controller.Observe(Empty, true);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Threat));
            clock.Time = 15;
            controller.Observe(Empty, true);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
        }

        [Test]
        public void RecentShotEscalatesNewlyPerceivedPursuer()
        {
            controller.Shot();
            clock.Time = 11;
            Observe(Source(seen: true, engaged: true));
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Combat));
        }

        [Test]
        public void NonlethalDamageDucksAndHoldsCombatWithoutEnemy()
        {
            Assert.That(controller.Damage(1).Success, Is.True);
            Assert.That(controller.IsDucking, Is.True);
            controller.Observe(Empty, true);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Combat));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void LethalDamageDoesNotStartANewCombatCue(int health)
        {
            Assert.That(controller.Damage(health).Success, Is.False);
            Assert.That(controller.IsDucking, Is.False);
            controller.Observe(Empty, true);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
        }

        [Test]
        public void UrgentCueRequiresPerceptionAndExtendsDuck()
        {
            Observe(Source(urgentUntil: 11));
            Assert.That(controller.IsDucking, Is.False);
            Observe(Source(heardUntil: 11, urgentUntil: 11));
            clock.Time = 10.3;
            Assert.That(controller.IsDucking, Is.True);
            clock.Time = 10.41;
            Assert.That(controller.IsDucking, Is.False);
        }

        [Test]
        public void DeathClearsPresentedCountsAndMood()
        {
            Observe(Source(seen: true, engaged: true, attacking: true));
            controller.Observe(Empty, false);
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
            Assert.That(view.Perceived, Is.Zero);
            Assert.That(view.Engaged, Is.Zero);
        }

        [Test]
        public void ResetClearsPersonalStateWithoutChangingAnotherDirector()
        {
            var other = new AudioMoodModel(Settings(), clock);
            other.Observe(new[] { Source(seen: true) }, true);
            Observe(Source(seen: true, engaged: true, attacking: true));
            controller.Shot();
            controller.Reset();
            Assert.That(view.Mood, Is.EqualTo(AudioMood.Explore));
            Assert.That(model.RememberedCount, Is.Zero);
            Assert.That(controller.IsDucking, Is.False);
            Assert.That(controller.ObservationDue, Is.True);
            Assert.That(other.RememberedCount, Is.EqualTo(1));
            Assert.That(other.Mood, Is.EqualTo(AudioMood.Threat));
        }

        [Test]
        public void ObservationCadenceUsesInjectedClock()
        {
            controller.Observe(Empty, true);
            Assert.That(controller.ObservationDue, Is.False);
            clock.Time = 10.13;
            Assert.That(controller.ObservationDue, Is.True);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1d)]
        public void InvalidClockIsRejectedBeforeObservationMutation(double time)
        {
            Observe(Source(seen: true));
            clock.Time = time;
            Assert.Throws<InvalidOperationException>(() => controller.Observe(Empty, true));
            Assert.That(model.RememberedCount, Is.EqualTo(1));
        }

        [Test]
        public void MissingModelCommandsRejectWithoutThrowing()
        {
            Assert.That(new AudioMoodCueCommand(null, true, 0).Execute().Success, Is.False);
            Assert.That(new ResetAudioMoodCommand(null).Execute().Success, Is.False);
        }

        [Test]
        public void WarmedObservationLoopDoesNotAllocateManagedMemory()
        {
            var sources = new[] { Source(seen: true, engaged: true) };
            for (int i = 0; i < 10; i++)
            {
                controller.Observe(sources, true);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                controller.Observe(sources, true);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        private void Observe(AudioThreatObservation source) => controller.Observe(new[] { source }, true);
        private static AudioMoodSettings Settings() => new AudioMoodSettings(.12f, 1.25f, 9, 2.5f, 2, 5, 3, 2, .22f, .6f, .4f);
        private static AudioThreatObservation Source(int id = 1, bool alive = true, bool eligible = true, float distanceSquared = 16,
            bool seen = false, float heardUntil = float.NegativeInfinity, float urgentUntil = float.NegativeInfinity,
            bool engaged = false, bool attacking = false) =>
            new AudioThreatObservation(id, alive, eligible, distanceSquared, seen, heardUntil, urgentUntil, engaged, attacking);

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class View : IAudioMoodView
        {
            public AudioMood Mood;
            public int Perceived;
            public int Engaged;
            public void ApplyMood(AudioMood value, int perceived, int engaged)
            {
                Mood = value;
                Perceived = perceived;
                Engaged = engaged;
            }
        }
    }
}
