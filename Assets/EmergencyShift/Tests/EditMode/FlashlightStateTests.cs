using System.Collections.Generic;
using EmergencyShift.Features.Flashlight;
using EmergencyShift.Features.Flashlight.Config;
using EmergencyShift.Features.Flashlight.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class FlashlightStateTests
    {
        [Test]
        public void AcquisitionAndRestorationPreserveEventOrderAndRejectDuplicateAcquisition()
        {
            var view = new LightView();
            FlashlightController light = Create(view, false);
            light.SetPower(true);
            Assert.That(light.IsOn, Is.False);
            Assert.That(light.Acquire(), Is.True);
            Assert.That(view.Events, Is.EqualTo(new[] { "owned", "on" }));
            Assert.That(light.IsOn, Is.True);
            Assert.That(light.Acquire(), Is.False);
            light.SetPower(true);
            Assert.That(view.Events.Count, Is.EqualTo(2));
            light.RestoreOwnership(false);
            Assert.That(view.Events, Is.EqualTo(new[] { "owned", "on", "off", "owned" }));
            light.RestoreOwnership(false);
            Assert.That(view.Events.Count, Is.EqualTo(4));
            light.RestoreOwnership(true);
            Assert.That(light.IsOn, Is.False);
            Assert.That(view.Events.Count, Is.EqualTo(5));
        }

        [Test]
        public void AcquisitionCommandRechecksOwnershipAtExecution()
        {
            var model = new FlashlightStateModel(false, false, new Vector2(4.2f, 80));
            var stale = new AcquireFlashlightStateCommand(model);
            Assert.That(stale.CanExecute().Success, Is.True);
            Assert.That(new AcquireFlashlightStateCommand(model).Execute().Success, Is.True);
            Assert.That(stale.Execute().Success, Is.False);
        }

        [Test]
        public void MissingBeamCannotSupplyDefensiveLightAndDoesNotAffectAnotherPlayer()
        {
            var factory = new FlashlightControllerFactory();
            var absent = new LightView { HasBeam = false };
            FlashlightController first = factory.Create(true, true, new Vector2(4.2f, 80), absent);
            var present = new LightView();
            FlashlightController second = factory.Create(true, false, new Vector2(4.2f, 80), present);
            first.SetPower(true);
            second.SetPower(true);
            Assert.That(first.IsOn, Is.False);
            Assert.That(second.IsOn, Is.True);
            Assert.That(first.Covers(Vector2.zero, Vector2.up, Vector2.up), Is.False);
            Assert.That(second.Covers(Vector2.zero, Vector2.up, Vector2.up), Is.True);
            first.RestoreOwnership(false);
            Assert.That(second.IsOwned, Is.True);
        }

        [Test]
        public void FiniteOpticsClampAtExistingBoundsWithoutResettingPowerOrDuplicateEvents()
        {
            var view = new LightView();
            FlashlightController light = Create(view, true);
            light.SetPower(true);
            Assert.That(light.SetOptics(new Vector2(-1, 999)).Success, Is.True);
            Assert.That(light.Range, Is.EqualTo(.5f));
            Assert.That(light.BeamAngle, Is.EqualTo(140));
            Assert.That(light.IsOn, Is.True);
            int changes = view.Events.Count;
            light.SetOptics(new Vector2(-2, 999));
            Assert.That(view.Events.Count, Is.EqualTo(changes));
        }

        [TestCase(float.NaN, 80)]
        [TestCase(float.PositiveInfinity, 80)]
        [TestCase(float.NegativeInfinity, 80)]
        [TestCase(4, float.NaN)]
        [TestCase(4, float.PositiveInfinity)]
        [TestCase(4, float.NegativeInfinity)]
        public void NonfiniteOpticsAreRejectedBeforeStateOrPresentation(float range, float angle)
        {
            var view = new LightView();
            FlashlightController light = Create(view, true);
            Assert.That(light.SetOptics(new Vector2(range, angle)).Success, Is.False);
            Assert.That(light.Range, Is.EqualTo(4.2f));
            Assert.That(light.BeamAngle, Is.EqualTo(80));
            Assert.That(view.Events, Is.Empty);
            Assert.That(view.OpticsUpdates, Is.Zero);
        }

        [Test]
        public void DefensiveCoverageRetainsInnerReachAngleAndPowerRequirements()
        {
            FlashlightController light = Create(new LightView(), true);
            light.SetPower(true);
            Assert.That(light.Covers(Vector2.zero, Vector2.up, Vector2.up * 3.7f), Is.True);
            Assert.That(light.Covers(Vector2.zero, Vector2.up, Vector2.up * 3.9f), Is.False);
            Vector2 inside = Quaternion.Euler(0, 0, 34) * Vector2.up;
            Vector2 outside = Quaternion.Euler(0, 0, 38) * Vector2.up;
            Assert.That(light.Covers(Vector2.zero, Vector2.up, inside), Is.True);
            Assert.That(light.Covers(Vector2.zero, Vector2.up, outside), Is.False);
            Assert.That(light.Covers(Vector2.zero, Vector2.up, Vector2.down), Is.False);
            Assert.That(light.Covers(Vector2.zero, Vector2.up, Vector2.zero), Is.True);
            Assert.That(light.Covers(Vector2.zero, Vector2.up, new Vector2(float.NaN, 0)), Is.False);
            light.SetPower(false);
            Assert.That(light.Covers(Vector2.zero, Vector2.up, Vector2.up), Is.False);
        }

        [Test]
        public void AimDeadZoneClearsMomentumAndSnapDoesNotShareItBetweenInstances()
        {
            var settings = new FlashlightAimSettings(.075f, 720, .25f);
            var port = new AimPort();
            var controller = new FlashlightAimController(new FlashlightAimModel(settings), port);
            controller.Smooth(Vector2.right * 10, .016f);
            Assert.That(port.Angle, Is.LessThan(0));
            int updates = port.Updates;
            controller.Smooth(new Vector2(.1f, 0), .016f);
            Assert.That(port.Updates, Is.EqualTo(updates));
            var independent = new AimPort { Angle = port.Angle };
            var fresh = new FlashlightAimController(new FlashlightAimModel(settings), independent);
            controller.Smooth(Vector2.right * 10, .016f);
            fresh.Smooth(Vector2.right * 10, .016f);
            Assert.That(port.Angle, Is.EqualTo(independent.Angle));
            controller.Snap(Vector2.up * 10);
            Assert.That(port.Angle, Is.EqualTo(0).Within(.0001));
            Assert.That(independent.Angle, Is.LessThan(0));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void FrozenOrInvalidAimTickDoesNotWritePose(float deltaTime)
        {
            var port = new AimPort();
            var controller = new FlashlightAimController(new FlashlightAimModel(new FlashlightAimSettings(.075f, 720, .25f)), port);
            controller.Smooth(Vector2.right, deltaTime);
            Assert.That(port.Updates, Is.Zero);
            controller.Snap(new Vector2(float.MaxValue, float.MaxValue));
            Assert.That(port.Updates, Is.Zero);
        }

        private static FlashlightController Create(LightView view, bool owned) =>
            new FlashlightControllerFactory().Create(owned, false, new Vector2(4.2f, 80), view);

        private sealed class LightView : IFlashlightView
        {
            public bool HasBeam { get; set; } = true;
            public int OpticsUpdates { get; private set; }
            public readonly List<string> Events = new List<string>();
            public void RenderPower(bool on) { }
            public void RenderOptics(Vector2 optics) => OpticsUpdates++;
            public void OwnershipChanged() => Events.Add("owned");
            public void PowerChanged(bool on) => Events.Add(on ? "on" : "off");
            public void OpticsChanged() => Events.Add("optics");
        }

        private sealed class AimPort : IFlashlightAimEnvironment
        {
            public bool IsAvailable => true;
            public Vector2 OwnerPosition => Vector2.zero;
            public float Angle { get; set; }
            public int Updates { get; private set; }
            public void SetAngle(float angle)
            {
                Angle = angle;
                Updates++;
            }
        }
    }
}
