using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.DeathScreen.Config;
using EmergencyShift.Features.DeathScreen.Logic;
using NUnit.Framework;

namespace EmergencyShift.Features.DeathScreen.Tests
{
    public sealed class DeathScreenTests
    {
        private RecordingEnvironment environment;
        private DeathScreenModel model;
        private DeathScreenController controller;

        [SetUp]
        public void SetUp()
        {
            environment = new RecordingEnvironment();
            model = new DeathScreenModel(new DeathScreenSettings(1.4f, .7f, .5f, .35f));
            controller = new DeathScreenController(model, environment);
        }

        [TearDown]
        public void TearDown()
        {
            controller.Dispose();
            Assert.That(environment.InputOwners, Is.EqualTo(2), "External input owners must survive.");
            Assert.That(environment.MotionOwners, Is.EqualTo(3), "External motion owners must survive.");
        }

        [Test]
        public void LivingPlayerCannotEnterAndCommandRevalidatesDeath()
        {
            var command = new DeathScreenCommand(controller, DeathScreenIntent.Enter);
            Assert.That(command.CanExecute().Success, Is.True);
            environment.IsDead = false;
            Assert.That(command.Execute().Success, Is.False);
            Assert.That(model.IsVisible, Is.False);
            Assert.That(environment.InputAcquisitions, Is.Zero);
        }

        [Test]
        public void DeathAcquiresInputBeforeClosingOtherWindowsAndOnlyOnce()
        {
            Assert.That(controller.Execute(DeathScreenIntent.Enter), Is.True);
            Assert.That(environment.ClosedWhileBlocked, Is.True);
            Assert.That(controller.Execute(DeathScreenIntent.Enter), Is.False);
            Assert.That(environment.InputAcquisitions, Is.EqualTo(1));
            Assert.That(environment.MotionAcquisitions, Is.Zero);
            Assert.That(environment.WindowsClosed, Is.True);
        }

        [Test]
        public void ExistingTargetDeadReactionCanFinishBeforeWorldFreezes()
        {
            controller.Execute(DeathScreenIntent.Enter);
            controller.Tick(.25f, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Entering));
            Assert.That(environment.MotionAcquisitions, Is.Zero);
            Assert.That(model.ContentOpacity, Is.Zero);
            controller.Tick(.5f, false);
            Assert.That(model.ContentOpacity, Is.Zero, "Actions stay hidden until the world is black.");
            Assert.That(environment.MotionAcquisitions, Is.Zero);
            controller.Tick(.66f, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Entering));
            Assert.That(model.ContentOpacity, Is.GreaterThan(0));
            controller.Tick(.5f, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Ready));
            Assert.That(environment.MotionOwners, Is.EqualTo(4));
        }

        [Test]
        public void HeldConfirmationCannotActivateNewlyAppearingMenu()
        {
            controller.Execute(DeathScreenIntent.Enter);
            controller.Tick(2, true);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Entering));
            Assert.That(model.ContentOpacity, Is.EqualTo(1));
            Assert.That(controller.Execute(DeathScreenIntent.Retry), Is.False);
            controller.Tick(0, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Ready));
        }

        [Test]
        public void NewSceneFenceWaitsTwoTicksAndHeldConfirmationWithoutFreezingWorld()
        {
            controller.BeginStartupFence();
            controller.Tick(0, false);
            controller.Tick(0, false);
            controller.Tick(0, true);
            Assert.That(environment.InputOwners, Is.EqualTo(3));
            Assert.That(environment.MotionOwners, Is.EqualTo(3));
            Assert.That(model.IsVisible, Is.False);
            controller.Tick(0, false);
            Assert.That(environment.InputOwners, Is.EqualTo(2));
        }

        [Test]
        public void DeathTakesOverStartupFenceWithoutAcquiringAnotherInputLease()
        {
            controller.BeginStartupFence();
            Ready();
            Assert.That(environment.InputAcquisitions, Is.EqualTo(1));
            Assert.That(environment.InputOwners, Is.EqualTo(3));
            Assert.That(environment.MotionOwners, Is.EqualTo(4));
        }

        [Test]
        public void RetryRequiresNewTickReleaseAndOutgoingFadeBeforeLoading()
        {
            Ready();
            Assert.That(controller.Execute(DeathScreenIntent.Retry), Is.True);
            Assert.That(controller.Execute(DeathScreenIntent.Retry), Is.False);
            controller.Tick(10, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.WaitingRetry));
            controller.Tick(10, true);
            Assert.That(environment.RetryRequests, Is.Zero);
            controller.Tick(0, false);
            controller.Tick(.34f, false);
            Assert.That(environment.RetryRequests, Is.Zero);
            controller.Tick(.02f, false);
            Assert.That(environment.RetryRequests, Is.EqualTo(1));
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Loading));
            Assert.That(controller.Execute(DeathScreenIntent.Retry), Is.False);
        }

        [Test]
        public void CheckpointSelectionIsPassedToReturnAdapter()
        {
            environment.HasCheckpoint = true;
            BeginRetry();
            Assert.That(environment.LastFromCheckpoint, Is.True);
        }

        [Test]
        public void LoadingFailureKeepsOwnLeasesAndAllowsSingleNewAttempt()
        {
            BeginRetry();
            environment.Complete(CommandResult.Fail("Файл не изменён."));
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Error));
            Assert.That(model.Error, Is.EqualTo("Файл не изменён."));
            Assert.That(environment.InputOwners, Is.EqualTo(3));
            Assert.That(environment.MotionOwners, Is.EqualTo(4));
            Assert.That(controller.Execute(DeathScreenIntent.Retry), Is.True);
            controller.Tick(0, false);
            controller.Tick(0, false);
            controller.Tick(.36f, false);
            Assert.That(environment.RetryRequests, Is.EqualTo(2));
        }

        [Test]
        public void QuitRequiresExplicitConfirmationAndCancelPreservesError()
        {
            BeginRetry();
            environment.Complete(CommandResult.Fail("Ошибка загрузки"));
            Assert.That(controller.Execute(DeathScreenIntent.ConfirmQuit), Is.False);
            Assert.That(controller.Execute(DeathScreenIntent.AskQuit), Is.True);
            Assert.That(environment.QuitRequests, Is.Zero);
            Assert.That(controller.Execute(DeathScreenIntent.CancelQuit), Is.True);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Error));
            Assert.That(model.Error, Is.EqualTo("Ошибка загрузки"));
        }

        [Test]
        public void ConfirmedQuitWaitsForReleaseAndEditorRejectionDoesNotResumeDeadPlayer()
        {
            Ready();
            controller.Execute(DeathScreenIntent.AskQuit);
            controller.Execute(DeathScreenIntent.ConfirmQuit);
            controller.Tick(0, false);
            controller.Tick(10, true);
            Assert.That(environment.QuitRequests, Is.Zero);
            controller.Tick(0, false);
            controller.Tick(.36f, false);
            Assert.That(environment.QuitRequests, Is.EqualTo(1));
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Error));
            Assert.That(environment.InputOwners, Is.EqualTo(3));
            Assert.That(environment.MotionOwners, Is.EqualTo(4));
        }

        [Test]
        public void ExternalLivingRestoreRevealsWorldButWaitsForReleaseBeforeUnlocking()
        {
            Ready();
            environment.IsDead = false;
            controller.ObserveLivingPlayer();
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Returning));
            controller.Tick(1, true);
            Assert.That(model.IsVisible, Is.True);
            Assert.That(environment.InputOwners, Is.EqualTo(3));
            controller.Tick(0, false);
            Assert.That(model.IsVisible, Is.False);
            Assert.That(environment.InputOwners, Is.EqualTo(2));
            Assert.That(environment.MotionOwners, Is.EqualTo(3));
            Assert.That(environment.WindowsClosed, Is.False);
        }

        [Test]
        public void SuccessfulReturnAdapterKeepsFenceUntilReadyAndReleased()
        {
            BeginRetry();
            environment.IsDead = false;
            controller.ObserveLivingPlayer();
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Loading));
            environment.Complete(CommandResult.Ok());
            controller.Tick(.36f, true);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Returning));
            controller.Tick(0, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Hidden));
        }

        [Test]
        public void RestoreFailureCanBlockAnAliveDefaultPlayerInsteadOfSilentlyStartingFresh()
        {
            environment.IsDead = false;
            environment.RestoreError = "Broken slot";
            controller.BeginStartupFence();
            Assert.That(controller.Execute(DeathScreenIntent.ShowRestoreError), Is.True);
            Assert.That(model.IsRecoveryError, Is.True);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Error));
            Assert.That(environment.MotionOwners, Is.EqualTo(4));
            Assert.That(controller.Execute(DeathScreenIntent.Retry), Is.True);
        }

        [Test]
        public void ClosingAnotherWindowFailureRollsBackOnlyOurCapture()
        {
            environment.ThrowOnClose = true;
            Assert.Throws<InvalidOperationException>(() => controller.Execute(DeathScreenIntent.Enter));
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Hidden));
            Assert.That(environment.InputOwners, Is.EqualTo(2));
            Assert.That(environment.WindowsClosed, Is.False);
        }

        [Test]
        public void CancelAndDisposeAreIdempotentAndLateCompletionCannotReopenUi()
        {
            BeginRetry();
            controller.Dispose();
            controller.Dispose();
            environment.Complete(CommandResult.Fail("Late callback"));
            controller.Tick(1, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Hidden));
            Assert.That(controller.Execute(DeathScreenIntent.Enter), Is.False);
        }

        [Test]
        public void FailedMotionCaptureDoesNotLeaveInputOrOtherWindowsCaptured()
        {
            environment.ThrowOnMotion = true;
            controller.Execute(DeathScreenIntent.Enter);
            Assert.Throws<InvalidOperationException>(() => controller.Tick(1.5f, false));
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Hidden));
            Assert.That(environment.InputOwners, Is.EqualTo(2));
            Assert.That(environment.WindowsClosed, Is.False);
        }

        [Test]
        public void SeparatePlayerModelsDoNotShareStateOrLeases()
        {
            var otherEnvironment = new RecordingEnvironment();
            var other = new DeathScreenController(new DeathScreenModel(model.Settings), otherEnvironment);
            try
            {
                Ready();
                Assert.That(other.Model.IsVisible, Is.False);
                Assert.That(otherEnvironment.InputOwners, Is.EqualTo(2));
                other.Execute(DeathScreenIntent.Enter);
                controller.Cancel();
                Assert.That(other.Model.IsVisible, Is.True);
                Assert.That(otherEnvironment.InputOwners, Is.EqualTo(3));
            }
            finally
            {
                other.Dispose();
            }
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-.1f)]
        public void InvalidUnscaledStepCannotAdvanceState(float step)
        {
            controller.Execute(DeathScreenIntent.Enter);
            Assert.Throws<ArgumentOutOfRangeException>(() => controller.Tick(step, false));
            Assert.That(model.Elapsed, Is.Zero);
        }

        [Test]
        public void CustomProfileControlsReadinessRatherThanHardcodedTimes()
        {
            var customEnvironment = new RecordingEnvironment();
            var custom = new DeathScreenController(
                new DeathScreenModel(new DeathScreenSettings(2, .1f, .2f, .8f)), customEnvironment);
            try
            {
                custom.Execute(DeathScreenIntent.Enter);
                custom.Tick(1.5f, false);
                Assert.That(custom.Model.Phase, Is.EqualTo(DeathScreenPhase.Entering));
                Assert.That(custom.Model.ContentOpacity, Is.Zero);
                Assert.That(customEnvironment.MotionAcquisitions, Is.Zero);
                custom.Tick(.5f, false);
                Assert.That(custom.Model.Phase, Is.EqualTo(DeathScreenPhase.Entering));
                custom.Tick(.21f, false);
                Assert.That(custom.Model.Phase, Is.EqualTo(DeathScreenPhase.Ready));
            }
            finally
            {
                custom.Dispose();
            }
        }

        [Test]
        public void FastClosureStartsOnDeathAndActionsAppearOnlyOnCompleteBlack()
        {
            var fastEnvironment = new RecordingEnvironment();
            var fastModel = new DeathScreenModel(new DeathScreenSettings(.32f, 0, 0, .35f));
            using (var fast = new DeathScreenController(fastModel, fastEnvironment))
            {
                fast.Execute(DeathScreenIntent.Enter);
                Assert.That(fastModel.WorldOpacity, Is.EqualTo(1), "The iris is drawn immediately.");
                Assert.That(fastModel.ClosureProgress, Is.Zero);
                Assert.That(fastModel.ContentOpacity, Is.Zero);
                fast.Tick(.16f, false);
                Assert.That(fastModel.ClosureProgress, Is.EqualTo(.5f).Within(.001f));
                Assert.That(fastModel.ContentOpacity, Is.Zero);
                Assert.That(fastModel.IsInteractive, Is.False);
                fast.Tick(.17f, false);
                Assert.That(fastModel.ClosureProgress, Is.EqualTo(1));
                Assert.That(fastModel.ContentOpacity, Is.EqualTo(1));
                Assert.That(fastModel.Phase, Is.EqualTo(DeathScreenPhase.Ready));
            }
        }

        [Test]
        public void ConfiguredTextFadeCannotBeginBeforeClosureEvenWithEarlierDelay()
        {
            var customEnvironment = new RecordingEnvironment();
            var customModel = new DeathScreenModel(new DeathScreenSettings(.32f, .01f, .1f, .35f));
            using (var custom = new DeathScreenController(customModel, customEnvironment))
            {
                custom.Execute(DeathScreenIntent.Enter);
                custom.Tick(.31f, false);
                Assert.That(customModel.ContentOpacity, Is.Zero);
                custom.Tick(.06f, false);
                Assert.That(customModel.ContentOpacity, Is.GreaterThan(0).And.LessThan(1));
                Assert.That(customModel.IsInteractive, Is.False);
                custom.Tick(.06f, false);
                Assert.That(customModel.IsInteractive, Is.True);
            }
        }

        [TestCase(0)]
        [TestCase(-.1f)]
        [TestCase(1.1f)]
        [TestCase(float.NaN)]
        public void InvalidIrisFeatherIsRejected(float feather)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeathScreenSettings(.32f, .32f, 0, .35f, feather));
        }

        [Test]
        public void FastVisualDoesNotCutOffExistingTargetDeathReactionWhenMenuChangesPhase()
        {
            var fastEnvironment = new RecordingEnvironment();
            var fastModel = new DeathScreenModel(new DeathScreenSettings(.32f, .32f, 0, .35f, .18f, 1.4f));
            using (var fast = new DeathScreenController(fastModel, fastEnvironment))
            {
                fast.Execute(DeathScreenIntent.Enter);
                fast.Tick(.33f, false);
                Assert.That(fastModel.IsInteractive, Is.True);
                Assert.That(fastEnvironment.MotionAcquisitions, Is.Zero);
                fast.Execute(DeathScreenIntent.AskQuit);
                fast.Tick(.6f, false);
                Assert.That(fastEnvironment.MotionAcquisitions, Is.Zero);
                fast.Execute(DeathScreenIntent.CancelQuit);
                fast.Tick(.5f, false);
                Assert.That(fastEnvironment.MotionAcquisitions, Is.EqualTo(1));
                Assert.That(fastModel.IsInteractive, Is.True);
            }
        }

        [Test]
        public void LivingRestoreBeforeMotionDelayCannotPauseTheWorldLater()
        {
            var fastEnvironment = new RecordingEnvironment();
            var fastModel = new DeathScreenModel(new DeathScreenSettings(.32f, .32f, 0, .35f, .18f, 1.4f));
            using (var fast = new DeathScreenController(fastModel, fastEnvironment))
            {
                fast.Execute(DeathScreenIntent.Enter);
                fast.Tick(.33f, false);
                fastEnvironment.IsDead = false;
                fast.ObserveLivingPlayer();
                fast.Tick(2, false);
                fast.Tick(2, false);
                Assert.That(fastModel.IsVisible, Is.False);
                Assert.That(fastEnvironment.MotionAcquisitions, Is.Zero);
            }
        }

        private void Ready()
        {
            Assert.That(controller.Execute(DeathScreenIntent.Enter), Is.True);
            controller.Tick(model.Settings.EntrySeconds + .1f, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Ready));
        }

        private void BeginRetry()
        {
            Ready();
            controller.Execute(DeathScreenIntent.Retry);
            controller.Tick(0, false);
            controller.Tick(0, false);
            controller.Tick(.36f, false);
            Assert.That(model.Phase, Is.EqualTo(DeathScreenPhase.Loading));
        }

        private sealed class RecordingEnvironment : IDeathScreenEnvironment
        {
            private Action<CommandResult> pending;
            public bool IsDead { get; set; } = true;
            public bool HasCheckpoint { get; set; }
            public string RestoreError { get; set; }
            public int InputOwners { get; private set; } = 2;
            public int MotionOwners { get; private set; } = 3;
            public int InputAcquisitions { get; private set; }
            public int MotionAcquisitions { get; private set; }
            public int RetryRequests { get; private set; }
            public int QuitRequests { get; private set; }
            public bool LastFromCheckpoint { get; private set; }
            public bool WindowsClosed { get; private set; }
            public bool ClosedWhileBlocked { get; private set; }
            public bool ThrowOnClose { get; set; }
            public bool ThrowOnMotion { get; set; }

            public IDisposable AcquireInput()
            {
                InputOwners++;
                InputAcquisitions++;
                return new Lease(() => InputOwners--);
            }

            public IDisposable AcquireMotion()
            {
                if (ThrowOnMotion)
                {
                    throw new InvalidOperationException("Motion capture failed");
                }
                MotionOwners++;
                MotionAcquisitions++;
                return new Lease(() => MotionOwners--);
            }

            public void CloseOtherWindows()
            {
                ClosedWhileBlocked = InputOwners > 2;
                WindowsClosed = true;
                if (ThrowOnClose)
                {
                    throw new InvalidOperationException("Close failed");
                }
            }

            public void RestoreOtherWindows() => WindowsClosed = false;

            public void BeginRetry(bool fromCheckpoint, Action<CommandResult> completed)
            {
                RetryRequests++;
                LastFromCheckpoint = fromCheckpoint;
                pending = completed;
            }

            public void Complete(CommandResult result)
            {
                Action<CommandResult> callback = pending;
                pending = null;
                callback(result);
            }

            public CommandResult Quit()
            {
                QuitRequests++;
                return CommandResult.Fail("Выход доступен в сборке.");
            }

            private sealed class Lease : IDisposable
            {
                private Action release;
                public Lease(Action release) => this.release = release;
                public void Dispose()
                {
                    Action owned = release;
                    release = null;
                    owned?.Invoke();
                }
            }
        }
    }
}
