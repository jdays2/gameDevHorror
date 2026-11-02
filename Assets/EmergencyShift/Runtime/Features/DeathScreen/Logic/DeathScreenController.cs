using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.DeathScreen.Logic
{
    /// <summary>Owns only this screen's leases; platform effects stay behind the environment.</summary>
    public sealed class DeathScreenController : IDisposable
    {
        private readonly IDeathScreenEnvironment environment;
        private IDisposable inputLease;
        private IDisposable motionLease;
        private int startupFrames;
        private bool waitForNextTick;
        private bool disposed;
        public DeathScreenModel Model { get; }

        public DeathScreenController(DeathScreenModel model, IDeathScreenEnvironment environment)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public bool Execute(DeathScreenIntent intent) => new DeathScreenCommand(this, intent).Execute().Success;

        public void BeginStartupFence()
        {
            if (!disposed && !Model.IsVisible)
            {
                AcquireInput();
                startupFrames = 2;
            }
        }

        public void Tick(float unscaledSeconds, bool confirmationHeld)
        {
            if (!float.IsFinite(unscaledSeconds) || unscaledSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledSeconds));
            }
            if (disposed)
            {
                return;
            }
            switch (Model.Phase)
            {
                case DeathScreenPhase.Hidden:
                    TickStartup(confirmationHeld);
                    break;
                case DeathScreenPhase.Entering:
                    Model.Advance(unscaledSeconds);
                    // Let the existing TargetDead/body-closing reaction finish under the veil.
                    if (Model.Elapsed >= Model.Settings.WorldFadeSeconds)
                    {
                        AcquireMotion();
                    }
                    if (Model.Elapsed >= Model.Settings.EntrySeconds && !confirmationHeld)
                    {
                        Model.SetPhase(DeathScreenPhase.Ready);
                    }
                    break;
                case DeathScreenPhase.WaitingRetry:
                case DeathScreenPhase.WaitingQuit:
                    TickWaiting(confirmationHeld);
                    break;
                case DeathScreenPhase.LeavingRetry:
                case DeathScreenPhase.LeavingQuit:
                    TickLeaving(unscaledSeconds);
                    break;
                case DeathScreenPhase.Loading:
                    Model.Advance(unscaledSeconds);
                    break;
                case DeathScreenPhase.Returning:
                    Model.Advance(unscaledSeconds);
                    if (Model.Elapsed >= Model.Settings.ExitFadeSeconds && !confirmationHeld)
                    {
                        Cancel();
                    }
                    break;
            }
        }

        public void ObserveLivingPlayer()
        {
            if (!disposed && Model.IsVisible && Model.Phase != DeathScreenPhase.Loading &&
                Model.Phase != DeathScreenPhase.Returning && !Model.IsRecoveryError)
            {
                Model.SetPhase(DeathScreenPhase.Returning);
            }
        }

        public void Cancel()
        {
            startupFrames = 0;
            waitForNextTick = false;
            try
            {
                Model.Reset();
            }
            finally
            {
                ReleaseContext();
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Cancel();
            }
        }

        internal CommandResult Validate(DeathScreenIntent intent)
        {
            if (disposed)
            {
                return CommandResult.Fail("The death screen scope is disposed.");
            }
            switch (intent)
            {
                case DeathScreenIntent.Enter:
                    return Permit(!Model.IsVisible && environment.IsDead);
                case DeathScreenIntent.ShowRestoreError:
                    return Permit(!Model.IsVisible && !string.IsNullOrEmpty(environment.RestoreError));
                case DeathScreenIntent.Retry:
                case DeathScreenIntent.AskQuit:
                    return Permit(Model.Phase == DeathScreenPhase.Ready || Model.Phase == DeathScreenPhase.Error);
                case DeathScreenIntent.CancelQuit:
                case DeathScreenIntent.ConfirmQuit:
                    return Permit(Model.Phase == DeathScreenPhase.ConfirmQuit);
                default:
                    return CommandResult.Fail("Unknown death screen intent.");
            }
        }

        internal void Apply(DeathScreenIntent intent)
        {
            switch (intent)
            {
                case DeathScreenIntent.Enter:
                case DeathScreenIntent.ShowRestoreError:
                    Enter(intent == DeathScreenIntent.ShowRestoreError);
                    break;
                case DeathScreenIntent.Retry:
                    WaitForRelease(DeathScreenPhase.WaitingRetry);
                    break;
                case DeathScreenIntent.AskQuit:
                    Model.AskQuit();
                    break;
                case DeathScreenIntent.CancelQuit:
                    Model.CancelQuit();
                    break;
                case DeathScreenIntent.ConfirmQuit:
                    WaitForRelease(DeathScreenPhase.WaitingQuit);
                    break;
            }
        }

        private void Enter(bool recoveryError)
        {
            AcquireInput();
            startupFrames = 0;
            try
            {
                environment.CloseOtherWindows();
                if (recoveryError)
                {
                    AcquireMotion();
                    Model.ShowError("Не удалось загрузить сохранение.\nФайл не изменён. Попробуйте снова.", true, true);
                }
                else
                {
                    Model.Enter(environment.HasCheckpoint);
                }
            }
            catch
            {
                Cancel();
                throw;
            }
        }

        private void TickStartup(bool held)
        {
            if (startupFrames > 0)
            {
                startupFrames--;
            }
            else if (!held && inputLease != null)
            {
                ReleaseContext();
            }
        }

        private void WaitForRelease(DeathScreenPhase phase)
        {
            waitForNextTick = true;
            Model.SetPhase(phase);
        }

        private void TickWaiting(bool held)
        {
            if (waitForNextTick)
            {
                waitForNextTick = false;
                return;
            }
            if (!held)
            {
                Model.SetPhase(Model.Phase == DeathScreenPhase.WaitingRetry
                    ? DeathScreenPhase.LeavingRetry : DeathScreenPhase.LeavingQuit);
            }
        }

        private void TickLeaving(float seconds)
        {
            bool retry = Model.Phase == DeathScreenPhase.LeavingRetry;
            Model.Advance(seconds);
            if (Model.Elapsed < Model.Settings.ExitFadeSeconds)
            {
                return;
            }
            Model.SetPhase(DeathScreenPhase.Loading);
            if (retry)
            {
                environment.BeginRetry(Model.HasCheckpoint, OnRetryCompleted);
            }
            else
            {
                CommandResult result = environment.Quit();
                if (!result.Success)
                {
                    Model.ShowError(result.Reason, Model.HasCheckpoint, Model.IsRecoveryError);
                }
            }
        }

        private void OnRetryCompleted(CommandResult result)
        {
            if (disposed || Model.Phase != DeathScreenPhase.Loading)
            {
                return;
            }
            if (result.Success)
            {
                Model.SetPhase(DeathScreenPhase.Returning);
            }
            else
            {
                Model.ShowError(result.Reason, Model.HasCheckpoint, Model.IsRecoveryError);
            }
        }

        private void AcquireInput()
        {
            inputLease ??= environment.AcquireInput() ??
                throw new InvalidOperationException("The death screen requires an input lease.");
        }

        private void AcquireMotion()
        {
            try
            {
                motionLease ??= environment.AcquireMotion() ??
                    throw new InvalidOperationException("The death screen requires a motion lease.");
            }
            catch
            {
                Cancel();
                throw;
            }
        }

        private void ReleaseContext()
        {
            IDisposable ownedMotion = motionLease;
            IDisposable ownedInput = inputLease;
            motionLease = null;
            inputLease = null;
            try
            {
                environment.RestoreOtherWindows();
            }
            finally
            {
                try
                {
                    ownedMotion?.Dispose();
                }
                finally
                {
                    ownedInput?.Dispose();
                }
            }
        }

        private static CommandResult Permit(bool allowed) => allowed
            ? CommandResult.Ok() : CommandResult.Fail("The current death screen state rejects this intent.");
    }
}
