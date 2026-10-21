using System;
using System.Collections.Generic;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Time;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class WeaponControllerTests
    {
        private static WeaponShotRequest Aim => new WeaponShotRequest(Vector2.zero, Vector2.up, Vector2.up * 10, false);

        [Test]
        public void EmptyMagazineReloadStartsAfterShotPresentationAndCommitsOnlyOnce()
        {
            using (var fixture = new Fixture())
            {
                fixture.Controller.Acquire(Pistol(), 3);
                fixture.Controller.Operation += fixture.View.RecordOperation;
                Assert.That(fixture.Controller.TryFire(Aim), Is.True);
                CollectionAssert.AreEqual(new[] { "shot", "MagazineReloadStarted" }, fixture.View.Events);
                Assert.That(fixture.Controller.Inventory.ActiveCycle.Phase, Is.EqualTo(WeaponPhase.Loading));
                fixture.Clock.Time = 1.41;
                fixture.Controller.Update(default);
                Assert.That(fixture.Controller.Inventory.ActiveCycle.Loaded, Is.EqualTo(1));
                Assert.That(fixture.Controller.Inventory.ActiveCycle.TotalAmmo, Is.EqualTo(2));
                fixture.Controller.Update(default);
                Assert.That(fixture.View.Events.FindAll(e => e == "MagazineLoaded").Count, Is.EqualTo(1));
                fixture.Controller.Operation -= fixture.View.RecordOperation;
            }
        }

        [Test]
        public void BlockedMuzzleConsumesOneRoundAndCreatesNoProjectileDirections()
        {
            using (var fixture = new Fixture())
            {
                fixture.Controller.Acquire(Shotgun(), 12);
                var blocked = new WeaponShotRequest(Vector2.up, Vector2.up, Vector2.up * 10, true);
                Assert.That(fixture.Controller.TryFire(blocked), Is.True);
                Assert.That(fixture.Controller.LastShot.Directions, Is.Empty);
                Assert.That(fixture.Controller.ShotsFired, Is.EqualTo(1));
                Assert.That(fixture.Controller.Inventory.ActiveCycle.TotalAmmo, Is.EqualTo(11));
                Assert.That(fixture.Controller.LastShot.Request.MuzzleBlocked, Is.True);
            }
        }

        [TestCase("zero")]
        [TestCase("origin")]
        [TestCase("target")]
        [TestCase("overflow")]
        public void InvalidEnvironmentRequestDoesNotSpendAmmoOrRng(string fault)
        {
            var random = new CountingRandom();
            var model = new WeaponOwnerModel(random);
            model.Inventory.Acquire(Shotgun(), 12);
            Vector2 origin = fault == "origin" ? new Vector2(float.NaN, 0) : Vector2.zero;
            Vector2 target = fault == "target" ? new Vector2(0, float.PositiveInfinity) : Vector2.up * 10;
            Vector2 direction = Vector2.up;
            if (fault == "zero")
            {
                direction = Vector2.zero;
            }
            else if (fault == "overflow")
            {
                direction = new Vector2(float.MaxValue, float.MaxValue);
            }

            var command = new FireOwnedWeaponCommand(model, 0, new WeaponShotRequest(origin, direction, target, false));
            Assert.That(command.CanExecute().Success, Is.False);
            Assert.That(command.Execute().Success, Is.False);
            Assert.That(random.Draws, Is.Zero);
            Assert.That(model.ShotsFired, Is.Zero);
            Assert.That(model.Inventory.ActiveCycle.TotalAmmo, Is.EqualTo(12));
            Assert.That(model.LastShot, Is.Null);
        }

        [Test]
        public void InputBlockFreezesWeaponTickWhileDeadOwnerStillFinishesExistingPump()
        {
            using (var fixture = new Fixture())
            {
                fixture.Controller.Acquire(Shotgun(), 12);
                fixture.Controller.TryFire(Aim);
                fixture.Gate.InputBlocked = true;
                fixture.Clock.Time = 1;
                fixture.Controller.Update(default);
                Assert.That(fixture.Controller.Inventory.ActiveCycle.Phase, Is.EqualTo(WeaponPhase.Pumping));
                Assert.That(fixture.Controller.TryFire(Aim), Is.False);
                fixture.Gate.InputBlocked = false;
                fixture.Gate.CanUseWeapon = false;
                fixture.Controller.Update(default);
                Assert.That(fixture.Controller.Inventory.ActiveCycle.Phase, Is.EqualTo(WeaponPhase.Ready));
                Assert.That(fixture.Controller.TryFire(Aim), Is.False);
                Assert.That(fixture.Controller.AddAmmo(2), Is.Zero);
            }
        }

        [Test]
        public void TwoSlotIntentsKeepTheirOrderAndPumpingPreventsHolstering()
        {
            using (var fixture = new Fixture())
            {
                fixture.Controller.Acquire(Pistol(), 3);
                fixture.Controller.Acquire(Shotgun(), 12);
                var selected = new List<int>();
                Action selection = () => selected.Add(fixture.Controller.Inventory.ActiveDefinition.SelectionSlot);
                fixture.Controller.SelectionChanged += selection;
                fixture.Controller.Update(new WeaponInputIntent(true, true, false, false, false));
                CollectionAssert.AreEqual(new[] { 0, 1 }, selected);
                fixture.Controller.TryFire(Aim);
                fixture.Clock.Time = .1;
                fixture.Controller.Update(new WeaponInputIntent(true, false, false, false, false));
                Assert.That(selected.Count, Is.EqualTo(2));
                fixture.Clock.Time = 1;
                fixture.Controller.Update(new WeaponInputIntent(true, false, false, false, false));
                CollectionAssert.AreEqual(new[] { 0, 1, 0 }, selected);
                fixture.Controller.SelectionChanged -= selection;
            }
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void AutomaticSettingChoosesHeldIntentWithoutReadingInputDevices(bool automatic, bool expected)
        {
            using (var fixture = new Fixture())
            {
                var settings = new WeaponSettings("intent", 0, 1, 20, .3f, 10, automatic);
                fixture.Controller.Acquire(settings, 3);
                bool requested = fixture.Controller.Update(new WeaponInputIntent(false, false, false, false, true));
                Assert.That(requested, Is.EqualTo(expected));
            }
        }

        [Test]
        public void EmptyTriggerIsThrottledAndBlockedOrDisposedOwnerCannotPlayIt()
        {
            using (var fixture = new Fixture())
            {
                fixture.Controller.Acquire(Pistol(), 0);
                fixture.Controller.TryFire(Aim);
                fixture.Controller.TryFire(Aim);
                Assert.That(fixture.View.EmptyCount, Is.EqualTo(1));
                fixture.Clock.Time = .249;
                fixture.Controller.TryFire(Aim);
                Assert.That(fixture.View.EmptyCount, Is.EqualTo(1));
                fixture.Clock.Time = .25;
                fixture.Controller.TryFire(Aim);
                Assert.That(fixture.View.EmptyCount, Is.EqualTo(2));
                fixture.Gate.InputBlocked = true;
                fixture.Clock.Time = 1;
                fixture.Controller.TryFire(Aim);
                fixture.Controller.Dispose();
                fixture.Gate.InputBlocked = false;
                fixture.Controller.TryFire(Aim);
                Assert.That(fixture.View.EmptyCount, Is.EqualTo(2));
                Assert.Throws<ObjectDisposedException>(() => fixture.Controller.ReplaceDefinition(Pistol()));
            }
        }

        [Test]
        public void DisposingUnsubscribesEvenWhenPresentationRejectsCancellation()
        {
            using (var fixture = new Fixture())
            {
                fixture.Controller.Acquire(Pistol(), 3);
                fixture.Controller.TryFire(Aim);
                WeaponCycle cycle = fixture.Controller.Inventory.ActiveCycle;
                int notifications = 0;
                Action<WeaponOperation> listener = operation =>
                {
                    notifications++;
                    if (operation == WeaponOperation.ReloadCancelled)
                    {
                        throw new InvalidOperationException("test presentation");
                    }
                };
                fixture.Controller.Operation += listener;
                Assert.Throws<InvalidOperationException>(fixture.Controller.Dispose);
                Assert.That(cycle.TryReload(0), Is.True);
                cycle.Tick(2);
                Assert.That(notifications, Is.EqualTo(1));
                fixture.Controller.Operation -= listener;
            }
        }

        [Test]
        public void CommittedVolleyIsImmutableBeforeCosmeticPresentation()
        {
            using (var fixture = new Fixture())
            {
                fixture.Controller.Acquire(Shotgun(), 12);
                Assert.That(fixture.Controller.TryFire(Aim), Is.True);
                WeaponShotPlan plan = fixture.View.LastShot;
                Assert.That(plan.Directions.Count, Is.EqualTo(8));
                Assert.That(plan.VolleyId, Is.EqualTo(1));
                Assert.Throws<NotSupportedException>(() => ((IList<Vector2>)plan.Directions).Clear());
            }
        }

        [TestCase("ready")]
        [TestCase("loading")]
        [TestCase("pumping")]
        public void PassiveControllerFramesDoNotAllocateCommandsWhileWaiting(string state)
        {
            using (var fixture = new Fixture())
            {
                WeaponSettings settings = Pistol();
                if (state == "pumping")
                {
                    settings = new PumpShotgunSettings(new MagazineWeaponSettings(
                        new WeaponSettings("pump", 0, 1, 120, .9f, 10, false), 1, .8f), 8, 18, .85f, .16f);
                }

                fixture.Controller.Acquire(settings, 3);
                if (state != "ready")
                {
                    fixture.Controller.TryFire(Aim);
                }

                for (int i = 0; i < 100; i++)
                {
                    fixture.Controller.Update(default);
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++)
                {
                    fixture.Controller.Update(default);
                }

                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(allocated, Is.Zero, "Only the pure controller loop is measured, not Unity rendering or FPS.");
            }
        }

        private static WeaponSettings Pistol() => new MagazineWeaponSettings(
            new WeaponSettings("pistol", 0, 1, 120, .3f, 10, false), 1, 1.4f);

        private static WeaponSettings Shotgun() => new PumpShotgunSettings(
            new MagazineWeaponSettings(new WeaponSettings("shotgun", 1, 1, 120, .9f, 10, false), 4, .8f),
            8, 18, .85f, .16f);

        private sealed class Fixture : IDisposable
        {
            public readonly TestClock Clock = new TestClock();
            public readonly TestGate Gate = new TestGate();
            public readonly TestView View = new TestView();
            public readonly WeaponController Controller;

            public Fixture()
            {
                Controller = new WeaponControllerFactory(Clock, new RandomSourceFactory(new System.Random(93))).Create(Gate, View);
            }

            public void Dispose() => Controller.Dispose();
        }

        private sealed class TestClock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class TestGate : IWeaponUseGate
        {
            public bool InputBlocked { get; set; }
            public bool CanUseWeapon { get; set; } = true;
        }

        private sealed class TestView : IWeaponShotView
        {
            public readonly List<string> Events = new List<string>();
            public WeaponShotPlan LastShot;
            public int EmptyCount;
            public void RecordOperation(WeaponOperation operation) => Events.Add(operation.ToString());

            public void PresentShot(WeaponShotPlan plan)
            {
                LastShot = plan;
                Events.Add("shot");
            }

            public void PresentEmptyTrigger() => EmptyCount++;
        }

        private sealed class CountingRandom : IRandomSource
        {
            public int Draws;
            public int NextInt(int minimumInclusive, int maximumExclusive) => minimumInclusive;

            public double NextUnit()
            {
                Draws++;
                return .5;
            }
        }
    }
}
