using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Doors.Config;
using UnityEngine;

namespace EmergencyShift.Features.Doors.Logic
{
    /// <summary>Personal door state and scaled-clock timeline, independent of leaf geometry and audio.</summary>
    public sealed class DoorMotionModel
    {
        private readonly DoorMotionSettings settings;
        private readonly IGameClock clock;
        private double accessAt;
        private double motionAt;
        private float startAmount;

        public bool IsOpen { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsAwaitingAccess { get; private set; }
        public bool CanInteract => !IsMoving && !IsAwaitingAccess;
        public float OpenAmount { get; private set; }
        public event Action<bool> MotionStarted;
        public event Action<bool> MotionStopped;

        public DoorMotionModel(DoorMotionSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        internal void RequestToggle(float confirmationDelay)
        {
            if (confirmationDelay > 0 && !IsOpen)
            {
                accessAt = clock.Time + confirmationDelay;
                IsAwaitingAccess = true;
                return;
            }

            BeginMotion();
        }

        public void Tick()
        {
            if (IsAwaitingAccess && clock.Time >= accessAt)
            {
                IsAwaitingAccess = false;
                BeginMotion();
            }

            if (!IsMoving)
            {
                return;
            }

            float progress = Mathf.Clamp01((float)((clock.Time - motionAt) / settings.Duration));
            OpenAmount = Mathf.Lerp(startAmount, IsOpen ? 1 : 0, Mathf.SmoothStep(0, 1, progress));
            if (progress >= 1)
            {
                IsMoving = false;
                MotionStopped?.Invoke(IsOpen);
            }
        }

        internal void Restore(bool open)
        {
            IsOpen = open;
            IsMoving = false;
            IsAwaitingAccess = false;
            OpenAmount = open ? 1 : 0;
        }

        internal void Stop()
        {
            bool wasMoving = IsMoving;
            Restore(IsOpen);
            if (wasMoving)
            {
                MotionStopped?.Invoke(IsOpen);
            }
        }

        private void BeginMotion()
        {
            IsOpen = !IsOpen;
            IsMoving = true;
            startAmount = OpenAmount;
            motionAt = clock.Time;
            MotionStarted?.Invoke(IsOpen);
        }
    }
}
