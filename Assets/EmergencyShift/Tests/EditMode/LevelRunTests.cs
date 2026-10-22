using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.LevelProgress;
using EmergencyShift.Features.LevelProgress.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class LevelRunTests
    {
        [Test]
        public void TimerUsesInjectedScaledClockAndDoesNotResetOnRepeatedStart()
        {
            var clock = new Clock { Now = 10 };
            var run = Run(clock, new Environment());
            Assert.That(run.ElapsedSeconds, Is.Zero);
            run.Start();
            clock.Now = 12;
            Assert.That(run.ElapsedSeconds, Is.EqualTo(2));
            run.Start();
            clock.Now = 15;
            Assert.That(run.ElapsedSeconds, Is.EqualTo(5));
        }

        [Test]
        public void RestoreBeforeStartIsNotOverwrittenAndPausedClockDoesNotAdvance()
        {
            var clock = new Clock { Now = 10 };
            var run = Run(clock, new Environment());
            run.RestoreElapsed(12);
            clock.Now = 15;
            run.Start();
            Assert.That(run.ElapsedSeconds, Is.EqualTo(17));
            Assert.That(run.ElapsedSeconds, Is.EqualTo(17));
            run.RestoreElapsed(-2);
            Assert.That(run.ElapsedSeconds, Is.Zero);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidElapsedRejectsWithoutStateChange(float elapsed)
        {
            var run = Run(new Clock(), new Environment());
            run.RestoreElapsed(12);
            Assert.That(run.RestoreElapsed(elapsed).Success, Is.False);
            Assert.That(run.ElapsedSeconds, Is.EqualTo(12));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1d)]
        public void InvalidClockThrowsBeforeChangingRestoredTime(double now)
        {
            var clock = new Clock();
            var run = Run(clock, new Environment());
            run.RestoreElapsed(12);
            clock.Now = now;
            Assert.Throws<InvalidOperationException>(() => run.RestoreElapsed(100));
            clock.Now = 0;
            Assert.That(run.ElapsedSeconds, Is.EqualTo(12));
        }

        [TestCase(false, false, false)]
        [TestCase(false, false, true)]
        [TestCase(false, true, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, false)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void FinishRejectsWrongActorDeathOrMissingAccess(bool ownsActor, bool alive, bool access)
        {
            var environment = new Environment();
            var run = Run(new Clock(), environment);
            Assert.That(run.TryFinish(ownsActor, alive, access).Success, Is.False);
            Assert.That(run.Completed, Is.False);
            Assert.That(environment.Stops, Is.Zero);
            Assert.That(run.Restart().Success, Is.False);
            Assert.That(environment.Restarts, Is.Zero);
        }

        [Test]
        public void FinishCommitsBeforeSceneStopAndNotifiesOnceAfterwards()
        {
            var environment = new Environment();
            var clock = new Clock();
            var run = Run(clock, environment);
            run.Start();
            int notifications = 0;
            environment.OnStop = () =>
            {
                Assert.That(run.Completed, Is.True);
                Assert.That(notifications, Is.Zero);
                Assert.That(run.TryFinish(true, true, true).Success, Is.False);
            };
            run.Finished += Count;
            try
            {
                Assert.That(run.TryFinish(true, true, true).Success, Is.True);
                Assert.That(run.TryFinish(true, true, true).Success, Is.False);
                Assert.That(environment.Stops, Is.EqualTo(1));
                Assert.That(notifications, Is.EqualTo(1));
                clock.Now = 5;
                Assert.That(run.ElapsedSeconds, Is.EqualTo(5), "Legacy elapsed time continues after completion.");
            }
            finally
            {
                run.Finished -= Count;
            }

            void Count() => notifications++;
        }

        [Test]
        public void RestartRejectsReentryAndReleasesGuardAfterEnvironmentFailure()
        {
            var environment = new Environment();
            var run = Run(new Clock(), environment);
            run.TryFinish(true, true, true);
            environment.OnRestart = () =>
            {
                Assert.That(run.Restart().Success, Is.False);
                throw new InvalidOperationException("Controlled scene failure");
            };
            Assert.Throws<InvalidOperationException>(() => run.Restart());
            environment.OnRestart = null;
            Assert.That(run.Restart().Success, Is.True);
            Assert.That(environment.Restarts, Is.EqualTo(2));
        }

        [Test]
        public void ScopedFactoryCreatesIndependentRunsAndReadsClockOnlyForTime()
        {
            var clock = new Clock();
            var factory = new LevelProgressFactory(clock);
            var first = factory.CreateRun(new Environment());
            var second = factory.CreateRun(new Environment());
            first.RestoreElapsed(10);
            Assert.That(clock.Reads, Is.EqualTo(1));
            first.TryFinish(true, true, true);
            Assert.That(clock.Reads, Is.EqualTo(1));
            Assert.That(second.Completed, Is.False);
            Assert.That(second.ElapsedSeconds, Is.Zero);
        }

        [Test]
        public void MissingCommandDependenciesRejectRatherThanMutating()
        {
            Assert.That(new StartLevelRunCommand(null).Execute().Success, Is.False);
            Assert.That(new RestoreRunElapsedCommand(null, 1).Execute().Success, Is.False);
            Assert.That(new FinishLevelRunCommand(null, true, true, true).Execute().Success, Is.False);
            Assert.That(new RestartLevelRunCommand(null, null).Execute().Success, Is.False);
        }

        private static LevelRunController Run(Clock clock, Environment environment) => new LevelProgressFactory(clock).CreateRun(environment);

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

        private sealed class Environment : ILevelRunEnvironment
        {
            public int Stops;
            public int Restarts;
            public Action OnStop;
            public Action OnRestart;

            public void StopParticipants()
            {
                Stops++;
                OnStop?.Invoke();
            }

            public void RestartScene()
            {
                Restarts++;
                OnRestart?.Invoke();
            }
        }
    }
}
