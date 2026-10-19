using System;

namespace EmergencyShift
{
    /// <summary>Modal owners hold a lease; releasing one owner cannot unblock another.</summary>
    public sealed class GameplayInputBlock
    {
        private int owners;
        public bool IsBlocked => owners > 0;

        public IDisposable Acquire()
        {
            owners++;
            return new Lease(this);
        }

        private sealed class Lease : IDisposable
        {
            private bool disposed;
            private readonly GameplayInputBlock owner;

            public Lease(GameplayInputBlock owner)
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
                owner.owners = Math.Max(0, owner.owners - 1);
            }
        }
    }
}
