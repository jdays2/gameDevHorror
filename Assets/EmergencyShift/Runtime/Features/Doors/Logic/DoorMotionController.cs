using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Doors.Logic
{
    public sealed class DoorMotionController : IDisposable
    {
        private readonly DoorMotionModel model;
        private readonly IDoorMotionView view;
        private bool disposed;
        public bool IsOpen => model.IsOpen;
        public bool IsMoving => model.IsMoving;
        public bool CanInteract => model.CanInteract;

        public DoorMotionController(DoorMotionModel model, IDoorMotionView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            model.MotionStarted += OnStarted;
            model.MotionStopped += OnStopped;
        }

        public CommandResult Toggle(bool unlocked, float confirmationDelay = 0)
        {
            ThrowIfDisposed();
            return new ToggleDoorCommand(model, unlocked, confirmationDelay).Execute();
        }

        public void Tick()
        {
            ThrowIfDisposed();
            if (model.CanInteract)
            {
                return;
            }

            model.Tick();
            view.ApplyOpenAmount(model.OpenAmount);
        }

        public void Restore(bool open)
        {
            ThrowIfDisposed();
            new RestoreDoorMotionCommand(model, open).Execute();
            view.ApplyOpenAmount(model.OpenAmount);
        }

        public void Stop()
        {
            ThrowIfDisposed();
            new StopDoorMotionCommand(model).Execute();
            view.ApplyOpenAmount(model.OpenAmount);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            model.MotionStarted -= OnStarted;
            model.MotionStopped -= OnStopped;
        }

        private void OnStarted(bool open) => view.MotionStarted(open);
        private void OnStopped(bool open)
        {
            view.ApplyOpenAmount(model.OpenAmount);
            view.MotionStopped(open);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(DoorMotionController));
            }
        }
    }
}
