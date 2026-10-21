using System;
using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    /// <summary>Owns modal leases and the confirmation fence, independently of Unity/UI.</summary>
    public sealed class DialogueController : IDisposable
    {
        private readonly GameplayInputBlock inputBlock;
        private readonly WorldMotionPause motionPause;
        private readonly IFrameClock clock;
        private DialogueRun run;
        private IDisposable inputLease;
        private IDisposable motionLease;
        private int changedFrame;
        private bool waitingForRelease;
        private bool disposed;

        public bool IsOpen => run != null && !run.IsComplete;
        public bool CanBegin => !disposed && inputLease == null && !inputBlock.IsBlocked;
        public int LineIndex => run == null ? -1 : run.LineIndex;
        public DialogueNode Current => IsOpen ? run.Current : null;
        public bool RequiresChoice => IsOpen && run.RequiresChoice;
        public event Action ContextAcquired;
        public event Action ContextReleased;
        public event Action<DialogueStep> Transitioned;

        public DialogueController(GameplayInputBlock inputBlock, WorldMotionPause motionPause, IFrameClock clock)
        {
            this.inputBlock = inputBlock ?? throw new ArgumentNullException(nameof(inputBlock));
            this.motionPause = motionPause ?? throw new ArgumentNullException(nameof(motionPause));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool TryBegin(DialogueGraph graph) => new BeginDialogueCommand(this, graph).Execute().Success;

        internal void ApplyBegin(DialogueGraph graph)
        {
            var next = new DialogueRun(graph);
            inputLease = inputBlock.Acquire();
            try
            {
                motionLease = motionPause.Acquire();
                run = next;
                changedFrame = clock.Frame;
                ContextAcquired?.Invoke();
                Transitioned?.Invoke(run.LastTransition);
            }
            catch
            {
                ReleaseContext();
                throw;
            }
        }

        public bool Advance()
        {
            if (!IsOpen || clock.Frame <= changedFrame || !run.Advance())
            {
                return false;
            }

            PublishTransition();
            return true;
        }

        public bool Choose(int index)
        {
            if (!IsOpen || clock.Frame <= changedFrame || !run.Choose(index))
            {
                return false;
            }

            PublishTransition();
            return true;
        }

        private void PublishTransition()
        {
            changedFrame = clock.Frame;
            if (run.IsComplete)
            {
                motionLease?.Dispose();
                motionLease = null;
                waitingForRelease = true;
            }

            try
            {
                Transitioned?.Invoke(run.LastTransition);
            }
            catch
            {
                ReleaseContext();
                throw;
            }
        }

        public void Tick(bool confirmationHeld)
        {
            if (waitingForRelease && clock.Frame > changedFrame + 1 && !confirmationHeld)
            {
                ReleaseContext();
            }
        }

        public void Cancel()
        {
            if (inputLease == null)
            {
                return;
            }

            bool cancelled = IsOpen;
            run?.Cancel();
            DialogueStep step = run == null ? default : run.LastTransition;
            try
            {
                if (cancelled)
                {
                    Transitioned?.Invoke(step);
                }
            }
            finally
            {
                ReleaseContext();
            }
        }

        private void ReleaseContext()
        {
            if (inputLease == null)
            {
                return;
            }

            waitingForRelease = false;
            run = null;
            IDisposable ownedInput = inputLease;
            IDisposable ownedMotion = motionLease;
            inputLease = null;
            motionLease = null;
            try
            {
                ownedMotion?.Dispose();
                ContextReleased?.Invoke();
            }
            finally
            {
                ownedInput.Dispose();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Cancel();
        }
    }
}
