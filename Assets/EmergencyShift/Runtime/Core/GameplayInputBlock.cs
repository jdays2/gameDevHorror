using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Modal owners hold a lease; releasing one owner cannot unblock another.</summary>
    public static class GameplayInputBlock
    {
        private static int owners;
        public static bool IsBlocked => owners > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            owners = 0;
        }

        public static IDisposable Acquire()
        {
            owners++;
            return new Lease();
        }

        private sealed class Lease : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                owners = Math.Max(0, owners - 1);
            }
        }
    }
}
