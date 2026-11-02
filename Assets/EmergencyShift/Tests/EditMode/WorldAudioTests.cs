using System;
using EmergencyShift.Features.WorldAudio.Config;
using EmergencyShift.Features.WorldAudio.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class WorldAudioTests
    {
        [Test]
        public void DeathReachesExactSilenceAndStaysThereDespiteRepeatedNotifications()
        {
            var model = Model();
            var output = new Output();
            using (var controller = new WorldAudioController(model, output))
            {
                controller.SetDefeated(true);
                controller.Step(.5f);
                Assert.That(output.Gain, Is.EqualTo(.5f).Within(.0001));
                controller.SetDefeated(true);
                controller.Step(.5f);
                Assert.That(output.Gain, Is.Zero);
                controller.Step(100);
                Assert.That(output.Gain, Is.Zero);
            }
        }

        [Test]
        public void RestoreDuringFadeStartsAtCurrentGainWithoutAJumpOrAccumulation()
        {
            var model = Model();
            var output = new Output();
            using (var controller = new WorldAudioController(model, output))
            {
                controller.SetDefeated(true);
                controller.Step(.5f);
                controller.SetDefeated(false);
                Assert.That(model.Gain, Is.EqualTo(.5f));
                controller.Step(.1f);
                Assert.That(model.Gain, Is.GreaterThan(.5f).And.LessThan(1));
                controller.Step(.9f);
                Assert.That(model.Gain, Is.EqualTo(1));
                for (int index = 0; index < 5; index++)
                {
                    controller.SetDefeated(true);
                    controller.Step(1);
                    Assert.That(model.Gain, Is.Zero);
                    controller.SetDefeated(false);
                    controller.Step(.25f);
                    Assert.That(model.Gain, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void NewDeathDuringRestoreDoesNotJumpBackToFullVolume()
        {
            var model = Model();
            new SetWorldAudioDefeatedCommand(model, true).Execute();
            model.Step(1);
            new SetWorldAudioDefeatedCommand(model, false).Execute();
            model.Step(.125f);
            float before = model.Gain;
            new SetWorldAudioDefeatedCommand(model, true).Execute();
            Assert.That(model.Gain, Is.EqualTo(before));
            model.Step(1);
            Assert.That(model.Gain, Is.Zero);
        }

        [Test]
        public void IndependentModelsAndDisposeReleaseOnlyTheirOwnOutput()
        {
            var first = new WorldAudioController(Model(), new Output());
            var secondOutput = new Output();
            using (var second = new WorldAudioController(Model(), secondOutput))
            {
                first.SetDefeated(true);
                first.Step(1);
                second.Step(1);
                Assert.That(second.Gain, Is.EqualTo(1));
                first.Dispose();
                first.Dispose();
                Assert.That(first.SetDefeated(false).Success, Is.False);
                Assert.That(secondOutput.Disposals, Is.Zero);
            }
            Assert.That(secondOutput.Disposals, Is.EqualTo(1));
            Assert.That(new SetWorldAudioDefeatedCommand(null, true).Execute().Success, Is.False);
        }

        [TestCase(WorldAudioFadeCurve.Linear)]
        [TestCase(WorldAudioFadeCurve.SmoothStep)]
        [TestCase(WorldAudioFadeCurve.SmootherStep)]
        public void EveryConfiguredCurveIsMonotonicAndEndsAtSilence(WorldAudioFadeCurve curve)
        {
            var model = new WorldAudioModel(new WorldAudioSettings(1, .25f, curve));
            new SetWorldAudioDefeatedCommand(model, true).Execute();
            float previous = 1;
            for (int index = 0; index < 100; index++)
            {
                model.Step(.02f);
                Assert.That(model.Gain, Is.InRange(0, previous));
                previous = model.Gain;
            }
            Assert.That(model.Gain, Is.Zero);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1)]
        public void InvalidPresentationTimeCannotMutateTheEnvelope(float seconds)
        {
            var model = Model();
            new SetWorldAudioDefeatedCommand(model, true).Execute();
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Step(seconds));
            Assert.That(model.Gain, Is.EqualTo(1));
        }

        [Test]
        public void InvalidTuningIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new WorldAudioSettings(float.NaN, .25f, WorldAudioFadeCurve.Linear));
            Assert.Throws<ArgumentException>(() => new WorldAudioSettings(1, 0, WorldAudioFadeCurve.Linear));
            Assert.Throws<ArgumentException>(() => new WorldAudioSettings(1, .25f, (WorldAudioFadeCurve)99));
        }

        private static WorldAudioModel Model() => new WorldAudioModel(new WorldAudioSettings(1, .25f, WorldAudioFadeCurve.SmoothStep));

        private sealed class Output : IWorldAudioGain
        {
            public float Gain = 1;
            public int Disposals;
            public void Apply(float value) => Gain = value;
            public void Dispose() => Disposals++;
        }
    }
}
