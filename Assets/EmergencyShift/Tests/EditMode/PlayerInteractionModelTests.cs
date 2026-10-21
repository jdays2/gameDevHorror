using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Interaction;
using EmergencyShift.Features.Interaction.Config;
using EmergencyShift.Features.Interaction.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class PlayerInteractionModelTests
    {
        [Test]
        public void NearbyDistanceTiePrefersFacingThenCloserDistanceWins()
        {
            var environment = new Environment
            {
                Candidates = new[] { Candidate(0, Vector2.right), Candidate(1, Vector2.up * 1.03f), Candidate(2, Vector2.up * 1.3f) }
            };
            InteractionSelectionController controller = Create(environment);
            controller.Refresh();
            Assert.That(environment.Presented, Is.EqualTo(1));
            environment.Candidates = new[] { Candidate(0, Vector2.up), Candidate(1, Vector2.right * .8f) };
            controller.Refresh();
            Assert.That(environment.Presented, Is.EqualTo(1));
        }

        [Test]
        public void WithoutFacingDistanceTiePreservesFirstCandidate()
        {
            var environment = new Environment
            {
                Candidates = new[] { new InteractionCandidate(0, true, Vector2.up, Vector2.zero, false),
                    new InteractionCandidate(1, true, Vector2.right * .98f, Vector2.zero, false) }
            };
            Create(environment).Refresh();
            Assert.That(environment.Presented, Is.Zero);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.MaxValue)]
        public void InvalidOrOverflowingGeometryCannotSelectAnObject(float x)
        {
            var environment = new Environment { Candidates = new[] { Candidate(0, new Vector2(x, x)) } };
            Create(environment).Refresh();
            Assert.That(environment.Presented, Is.EqualTo(-1));
        }

        [Test]
        public void CommandRechecksActorAndTargetBeforeApplying()
        {
            var environment = new Environment();
            InteractionSelectionController controller = Create(environment);
            controller.Refresh();
            var command = new InteractSelectionCommand(controller.Model, environment);
            Assert.That(command.CanExecute().Success, Is.True);
            environment.CanAct = false;
            Assert.That(command.Execute().Success, Is.False);
            environment.CanAct = true;
            environment.Available = false;
            Assert.That(command.Execute().Success, Is.False);
            Assert.That(environment.Actions, Is.Zero);
            environment.Available = true;
            Assert.That(command.Execute().Success, Is.True);
            Assert.That(environment.Actions, Is.EqualTo(1));
        }

        [Test]
        public void RefreshClearsOldSelectionWhenBlockedAndWhenCandidatesDisappear()
        {
            var environment = new Environment();
            InteractionSelectionController controller = Create(environment);
            controller.Refresh();
            Assert.That(environment.Presented, Is.Zero);
            environment.CanAct = false;
            controller.Refresh();
            Assert.That(environment.Presented, Is.EqualTo(-1));
            environment.CanAct = true;
            environment.Candidates = Array.Empty<InteractionCandidate>();
            Assert.That(controller.TryInteract().Success, Is.False);
            Assert.That(environment.Actions, Is.Zero);
        }

        [Test]
        public void ReachUsesBothActorAndTargetRangeAndRejectsMalformedValues()
        {
            InteractionSelectionModel model = Create(new Environment()).Model;
            Assert.That(model.WithinReach(Vector2.right, 2), Is.True);
            Assert.That(model.WithinReach(Vector2.right * 2, 3), Is.False);
            Assert.That(model.WithinReach(Vector2.right, .5f), Is.False);
            Assert.That(model.WithinReach(Vector2.zero, -1), Is.False);
            Assert.That(model.WithinReach(new Vector2(float.NaN, 0), 2), Is.False);
        }

        [Test]
        public void OnePlayersRefreshAndClearDoNotChangeAnotherSelection()
        {
            var factory = new PlayerInteractionControllerFactory(new Clock());
            InteractionSelectionController a = factory.Create(Settings(), new Environment());
            InteractionSelectionController b = factory.Create(Settings(), new Environment());
            a.Refresh();
            b.Refresh();
            a.Clear();
            Assert.That(a.Model.CurrentIndex, Is.EqualTo(-1));
            Assert.That(b.Model.CurrentIndex, Is.Zero);
        }

        [Test]
        public void NoticeQueueIsBoundedAndDropsOldestPendingMessage()
        {
            var clock = new Clock();
            var model = new PickupNoticeModel(Settings(2), clock);
            Show(model, "A");
            Show(model, "B");
            Show(model, "C");
            Show(model, "D");
            Assert.That(model.Current, Is.EqualTo("A"));
            Assert.That(model.PendingCount, Is.EqualTo(2));
            clock.Time = 2;
            model.Tick();
            Assert.That(model.Current, Is.EqualTo("C"));
            clock.Time = 4;
            model.Tick();
            Assert.That(model.Current, Is.EqualTo("D"));
            clock.Time = 6;
            Assert.That(model.Current, Is.Null);
        }

        [Test]
        public void ExpiredNoticeDoesNotDequeueUntilOwnerTicksAndDisableClearRemovesAllState()
        {
            var clock = new Clock();
            var model = new PickupNoticeModel(Settings(), clock);
            Show(model, "A");
            Show(model, "B");
            clock.Time = 10;
            Assert.That(model.Current, Is.Null);
            Assert.That(model.PendingCount, Is.EqualTo(1));
            model.Tick();
            Assert.That(model.Current, Is.EqualTo("B"));
            model.Clear();
            Assert.That(model.Current, Is.Null);
            Assert.That(model.PendingCount, Is.Zero);
            Show(model, "C");
            Assert.That(model.Current, Is.EqualTo("C"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void EmptyNoticeCannotChangeQueue(string message)
        {
            var model = new PickupNoticeModel(Settings(), new Clock());
            Assert.That(new ShowPickupNoticeCommand(model, message).Execute().Success, Is.False);
            Assert.That(model.PendingCount, Is.Zero);
            Assert.That(model.Current, Is.Null);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1)]
        public void NoticeCommandRechecksItsClock(double invalid)
        {
            var clock = new Clock();
            var model = new PickupNoticeModel(Settings(), clock);
            var command = new ShowPickupNoticeCommand(model, "A");
            Assert.That(command.CanExecute().Success, Is.True);
            clock.Time = invalid;
            Assert.That(command.Execute().Success, Is.False);
            Assert.That(model.PendingCount, Is.Zero);
        }

        [Test]
        public void NotificationQueuesArePersonal()
        {
            var factory = new PlayerInteractionControllerFactory(new Clock());
            PickupNoticeModel a = factory.CreateNotices(Settings());
            PickupNoticeModel b = factory.CreateNotices(Settings());
            Show(a, "A");
            Show(b, "B");
            a.Clear();
            Assert.That(b.Current, Is.EqualTo("B"));
        }

        [TestCase("reach")]
        [TestCase("tie")]
        [TestCase("duration")]
        [TestCase("capacity")]
        public void InvalidInteractionTuningCannotEnterModels(string fault)
        {
            Assert.Throws<ArgumentException>(() => new PlayerInteractionSettings(fault == "reach" ? float.NaN : 1.6f,
                fault == "tie" ? -1 : .05f, fault == "duration" ? float.PositiveInfinity : 1.8f, fault == "capacity" ? 0 : 8));
        }

        private static void Show(PickupNoticeModel model, string message) => Assert.That(new ShowPickupNoticeCommand(model, message).Execute().Success, Is.True);
        private static PlayerInteractionSettings Settings(int maximum = 8) => new PlayerInteractionSettings(1.6f, .05f, 1.8f, maximum);
        private static InteractionCandidate Candidate(int index, Vector2 offset) => new InteractionCandidate(index, true, offset, Vector2.up, true);
        private static InteractionSelectionController Create(Environment environment) => new PlayerInteractionControllerFactory(new Clock()).Create(Settings(), environment);

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Environment : IInteractionEnvironment
        {
            public bool CanAct { get; set; } = true;
            public bool Available = true;
            public int Actions;
            public int Presented = -1;
            public InteractionCandidate[] Candidates = { Candidate(0, Vector2.up) };
            public int ScanCandidates() => Candidates.Length;
            public InteractionCandidate Observe(int index) => Candidates[index];
            public bool IsAvailable(int index) => Available;
            public bool Interact(int index)
            {
                Actions++;
                return true;
            }

            public void PresentTarget(int index) => Presented = index;
        }
    }
}
