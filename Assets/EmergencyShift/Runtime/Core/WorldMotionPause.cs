using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Scoped scaled-world freeze. Ambient playback is independent from motion.</summary>
    public static class WorldMotionPause
    {
        private static int owners;
        private static float previousScale;
        public static bool IsActive => owners > 0;
        public static bool PreservesAudioPlayback => IsActive && previousScale > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            owners = 0;
            previousScale = 1;
        }

        public static IDisposable Acquire()
        {
            if (owners == 0)
            {
                previousScale = Time.timeScale;
            }

            owners++;
            Time.timeScale = 0;
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
                owners--;
                if (owners == 0)
                {
                    Time.timeScale = previousScale;
                }
            }
        }
    }
}
