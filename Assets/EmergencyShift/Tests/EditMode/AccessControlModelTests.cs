using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AccessControl;
using EmergencyShift.Features.AccessControl.Config;
using EmergencyShift.Features.AccessControl.Logic;
using NUnit.Framework;
using Zenject;

namespace EmergencyShift.Tests
{
    public sealed class AccessControlModelTests
    {
        [Test]
        public void AddRevalidatesAfterAnotherCommandAcquiresTheSameCard()
        {
            var model = new AccessCardModel();
            var stale = new AddAccessCardCommand(model, "blue");
            Assert.That(stale.CanExecute().Success, Is.True);
            Assert.That(new AddAccessCardCommand(model, "blue").Execute().Success, Is.True);
            Assert.That(stale.Execute().Success, Is.False);
            Assert.That(model.Count, Is.EqualTo(1));
        }

        [Test]
        public void RestoreOwnsItsRequestAndCaptureDoesNotExposeLiveState()
        {
            var model = new AccessCardModel();
            string[] values = { "z", "a" };
            var command = new RestoreAccessCardsCommand(model, values);
            values[0] = "a";
            Assert.That(command.Execute().Success, Is.True);
            string[] captured = model.Capture();
            Assert.That(captured, Is.EqualTo(new[] { "a", "z" }));
            captured[0] = "foreign";
            Assert.That(model.Has("a"), Is.True);
            Assert.That(model.Has("foreign"), Is.False);
        }

        [Test]
        public void SharedFactoryMakesSeparateInventoriesAndOnlyPublishesAcceptedChanges()
        {
            var scope = new DiContainer();
            AccessControlInstaller.Install(scope);
            var factory = scope.Resolve<AccessControlFactory>();
            var firstView = new CardView();
            var secondView = new CardView();
            AccessCardController first = factory.CreateCards(firstView);
            AccessCardController second = factory.CreateCards(secondView);
            Assert.That(first.Add("blue").Success, Is.True);
            Assert.That(first.Add("blue").Success, Is.False);
            Assert.That(first.Add(" ").Success, Is.False);
            Assert.That(second.Has("blue"), Is.False);
            Assert.That(firstView.Changes, Is.EqualTo(1));
            Assert.That(secondView.Changes, Is.Zero);
        }

        [Test]
        public void LegacyNullSnapshotClearsOnlyItsOwnerAndNotifiesOnce()
        {
            var view = new CardView();
            var controller = new AccessControlFactory().CreateCards(view);
            controller.Add("blue");
            Assert.That(controller.Restore(null).Success, Is.True);
            Assert.That(controller.Count, Is.Zero);
            Assert.That(view.Changes, Is.EqualTo(2));
        }

        [Test]
        public void DenialAndNoticeHaveIndependentExactClockBoundaries()
        {
            var clock = new Clock();
            var model = new DoorAccessModel(new DoorAccessSettings("blue", .8, 1.8));
            var controller = new DoorAccessController(model, clock);
            DoorAccessAttempt first = controller.TryUnlock(false, true);
            Assert.That(first.Granted, Is.False);
            Assert.That(first.HasFeedback && first.ShowNotice, Is.True);
            clock.Time = .799;
            DoorAccessAttempt early = controller.TryUnlock(false, true);
            Assert.That(early.HasFeedback || early.ShowNotice, Is.False);
            clock.Time = .8;
            DoorAccessAttempt feedback = controller.TryUnlock(false, true);
            Assert.That(feedback.HasFeedback, Is.True);
            Assert.That(feedback.ShowNotice, Is.False);
            clock.Time = 1.8;
            Assert.That(controller.TryUnlock(false, true).ShowNotice, Is.True);
        }

        [Test]
        public void MissingNoticeRecipientDoesNotConsumeTheNoticeCooldown()
        {
            var model = new DoorAccessModel(new DoorAccessSettings("blue", .8, 1.8));
            var controller = new DoorAccessController(model, new Clock());
            Assert.That(controller.TryUnlock(false, false).ShowNotice, Is.False);
            Assert.That(controller.TryUnlock(false, true).ShowNotice, Is.True);
        }

        [Test]
        public void UnlockIsPermanentAndSilentRestoreResetsReaderCooldowns()
        {
            var controller = new DoorAccessController(new DoorAccessModel(new DoorAccessSettings("blue", .8, 1.8)), new Clock());
            DoorAccessAttempt grant = controller.TryUnlock(true, true);
            Assert.That(grant.Granted && grant.HasFeedback, Is.True);
            DoorAccessAttempt repeated = controller.TryUnlock(false, true);
            Assert.That(repeated.Granted, Is.True);
            Assert.That(repeated.HasFeedback || repeated.ShowNotice, Is.False);
            controller.Restore(false);
            Assert.That(controller.IsUnlocked, Is.False);
            Assert.That(controller.TryUnlock(false, true).HasFeedback, Is.True);
            controller.Restore(false);
            Assert.That(controller.TryUnlock(false, true).HasFeedback, Is.True);
        }

        [Test]
        public void ReadersFromOneScopeDoNotShareUnlockOrCooldowns()
        {
            var clock = new Clock();
            var factory = new DoorAccessControllerFactory(clock);
            var settings = new DoorAccessSettings("blue", .8, 1.8);
            DoorAccessController first = factory.Create(settings);
            DoorAccessController second = factory.Create(settings);
            first.TryUnlock(true, true);
            Assert.That(second.IsUnlocked, Is.False);
            Assert.That(second.TryUnlock(false, true).HasFeedback, Is.True);
            Assert.That(first.IsUnlocked, Is.True);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-1)]
        public void InvalidClockCannotUnlockOrConsumeFeedback(double time)
        {
            var clock = new Clock { Time = time };
            var controller = new DoorAccessController(new DoorAccessModel(new DoorAccessSettings("blue", .8, 1.8)), clock);
            Assert.That(controller.TryUnlock(true, true).Granted, Is.False);
            Assert.That(controller.IsUnlocked, Is.False);
            clock.Time = 0;
            Assert.That(controller.TryUnlock(false, true).HasFeedback, Is.True);
        }

        [Test]
        public void ReconfigurationKeepsUnlockStateAndOwnedCooldowns()
        {
            var controller = new DoorAccessController(new DoorAccessModel(new DoorAccessSettings("blue", .8, 1.8)), new Clock());
            controller.TryUnlock(false, true);
            controller.Configure(new DoorAccessSettings("green", 1, 2));
            Assert.That(controller.TryUnlock(false, true).HasFeedback, Is.False);
            controller.TryUnlock(true, true);
            controller.Configure(new DoorAccessSettings("red", 1, 2));
            Assert.That(controller.IsUnlocked, Is.True);
        }

        private sealed class CardView : IAccessCardView
        {
            public int Changes { get; private set; }
            public void CardsChanged() => Changes++;
        }

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }
    }
}
