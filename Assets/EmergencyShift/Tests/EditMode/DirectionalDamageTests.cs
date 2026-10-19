using System;
using System.Collections.Generic;
using EmergencyShift.Features.DamageFeedback.Config;
using EmergencyShift.Features.DamageFeedback.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Features.DamageFeedback.Tests
{
    public sealed class DirectionalDamageTests
    {
        private sealed class RecordingView : IDirectionalDamageView
        {
            public int Renders { get; private set; }
            public bool Visible { get; private set; }

            public void Render(IReadOnlyList<DamagePulse> pulses)
            {
                Renders++;
                Visible = false;
                foreach (DamagePulse pulse in pulses)
                {
                    Visible |= pulse.Opacity > 0;
                }
            }
        }

        private static DirectionalDamageModel CreateModel() =>
            new DirectionalDamageModel(new DamageFeedbackSettings(.1f, .9f, .6f));

        [TestCase(-1, 0, 0, .5f)]
        [TestCase(1, 0, 1, .5f)]
        [TestCase(0, 1, .5f, 1)]
        [TestCase(0, -1, .5f, 0)]
        [TestCase(-1, 1, 0, 1)]
        [TestCase(1, 1, 1, 1)]
        [TestCase(-1, -1, 0, 0)]
        [TestCase(1, -1, 1, 0)]
        public void SourceDirectionMeetsTheCorrectScreenEdge(float x, float y, float edgeX, float edgeY)
        {
            DirectionalDamageModel model = CreateModel();
            Assert.That(new ShowDamagePulseCommand(model, 1, new Vector2(x, y)).Execute().Success, Is.True);
            Assert.That(model.Pulses[0].EdgePoint.x, Is.EqualTo(edgeX).Within(.0001f));
            Assert.That(model.Pulses[0].EdgePoint.y, Is.EqualTo(edgeY).Within(.0001f));
        }

        [Test]
        public void RejectedDamageAndUnknownDirectionsDoNotInventAnIndicator()
        {
            DirectionalDamageModel model = CreateModel();
            Assert.That(new ShowDamagePulseCommand(model, 0, Vector2.left).Execute().Success, Is.False);
            Assert.That(new ShowDamagePulseCommand(model, -1, Vector2.left).Execute().Success, Is.False);
            Assert.That(new ShowDamagePulseCommand(model, 1, Vector2.zero).Execute().Success, Is.False);
            Assert.That(new ShowDamagePulseCommand(model, 1, new Vector2(float.NaN, 1)).Execute().Success, Is.False);
            Assert.That(new ShowDamagePulseCommand(model, 1, new Vector2(1, float.PositiveInfinity)).Execute().Success, Is.False);
            Assert.That(model.ActiveCount, Is.Zero);
        }

        [Test]
        public void ValidationAloneDoesNotShowDamage()
        {
            DirectionalDamageModel model = CreateModel();
            Assert.That(new ShowDamagePulseCommand(model, 1, Vector2.left).CanExecute().Success, Is.True);
            Assert.That(model.ActiveCount, Is.Zero);
        }

        [Test]
        public void DifferentDirectionsKeepTheirOwnFadeTimers()
        {
            DirectionalDamageModel model = CreateModel();
            new ShowDamagePulseCommand(model, 1, Vector2.left).Execute();
            model.Advance(.5f);
            new ShowDamagePulseCommand(model, 1, Vector2.up).Execute();
            model.Advance(.6f);
            Assert.That(model.ActiveCount, Is.EqualTo(1));
            Assert.That(model.Pulses[0].Opacity, Is.Zero);
            Assert.That(model.Pulses[1].Opacity, Is.GreaterThan(0));
            Assert.That(model.Pulses[1].EdgePoint, Is.EqualTo(new Vector2(.5f, 1)));
        }

        [Test]
        public void PeakIsHeldThenFadesWithoutExtendingTheFirstHit()
        {
            DirectionalDamageModel model = CreateModel();
            new ShowDamagePulseCommand(model, 1, Vector2.left).Execute();
            model.Advance(.05f);
            Assert.That(model.Pulses[0].Opacity, Is.EqualTo(.6f));
            model.Advance(.5f);
            Assert.That(model.Pulses[0].Opacity, Is.EqualTo(.3f).Within(.0001f));
            model.Advance(1);
            Assert.That(model.ActiveCount, Is.Zero);
        }

        [Test]
        public void MoreThanCapacityReplacesOldestPulsesWithoutGrowingState()
        {
            DirectionalDamageModel model = CreateModel();
            new ShowDamagePulseCommand(model, 1, Vector2.left).Execute();
            for (int i = 0; i < DirectionalDamageModel.Capacity; i++)
            {
                new ShowDamagePulseCommand(model, 1, Vector2.right).Execute();
            }

            Assert.That(model.ActiveCount, Is.EqualTo(DirectionalDamageModel.Capacity));
            Assert.That(model.Pulses.Count, Is.EqualTo(DirectionalDamageModel.Capacity));
            foreach (DamagePulse pulse in model.Pulses)
            {
                Assert.That(pulse.EdgePoint.x, Is.EqualTo(1));
            }
        }

        [Test]
        public void HudInstancesWithTheSameSettingsDoNotShareHits()
        {
            DirectionalDamageModel first = CreateModel();
            DirectionalDamageModel second = CreateModel();
            new ShowDamagePulseCommand(first, 1, Vector2.left).Execute();
            new ShowDamagePulseCommand(second, 1, Vector2.right).Execute();
            first.Clear();
            Assert.That(second.ActiveCount, Is.EqualTo(1));
            Assert.That(second.Pulses[0].Opacity, Is.EqualTo(.6f));
        }

        [Test]
        public void ControllerClearsTheLastFrameAndStopsRenderingWhenIdle()
        {
            var view = new RecordingView();
            var controller = new DirectionalDamageController(CreateModel(), view);
            controller.Show(1, Vector2.left);
            Assert.That(view.Visible, Is.True);
            controller.Tick(2);
            Assert.That(view.Visible, Is.False);
            int lastRender = view.Renders;
            controller.Tick(.1f);
            Assert.That(view.Renders, Is.EqualTo(lastRender));
            controller.Show(1, Vector2.up);
            controller.Reset();
            Assert.That(view.Visible, Is.False);
        }

        [Test]
        public void InvalidSettingsAndTimeAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DamageFeedbackSettings(-1, 1, .6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DamageFeedbackSettings(0, 0, .6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DamageFeedbackSettings(0, 1, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DirectionalDamageModel(default));
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateModel().Advance(float.PositiveInfinity));
        }
    }
}
