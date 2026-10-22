#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Owns temporary native measurement settings, including interrupted probe runs.</summary>
    public sealed class NativeBenchmarkSettingsLease : IDisposable
    {
        private readonly bool runInBackground = Application.runInBackground;
        private readonly int vSyncCount = QualitySettings.vSyncCount;
        private readonly int targetFrameRate = Application.targetFrameRate;
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Application.runInBackground = runInBackground;
            QualitySettings.vSyncCount = vSyncCount;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
#endif
