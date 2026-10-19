using System;
using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    /// <summary>Scoped scaled-world freeze. Ambient playback is independent from motion.</summary>
    public sealed class WorldMotionPause : IDisposable
    {
        private readonly IWorldTimeScale timeScale;
        private int owners;
        private float previousScale;
        private bool disposed;
        public bool IsActive => owners > 0;
        public bool PreservesAudioPlayback => IsActive && previousScale > 0;

        public WorldMotionPause(IWorldTimeScale timeScale)
        {
            this.timeScale = timeScale ?? throw new ArgumentNullException(nameof(timeScale));
        }

        public IDisposable Acquire()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldMotionPause));
            }

            if (owners == 0)
            {
                previousScale = timeScale.Scale;
            }

            owners++;
            timeScale.Scale = 0;
            return new Lease(this);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (owners > 0)
            {
                owners = 0;
                timeScale.Scale = previousScale;
            }
        }

        private void Release()
        {
            if (disposed)
            {
                return;
            }

            owners--;
            if (owners == 0)
            {
                timeScale.Scale = previousScale;
            }
        }

        private sealed class Lease : IDisposable
        {
            private bool disposed;
            private readonly WorldMotionPause owner;

            public Lease(WorldMotionPause owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                owner.Release();
            }
        }
    }
}
