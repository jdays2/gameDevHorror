using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.DeathScreen.Logic
{
    public interface IDeathScreenEnvironment
    {
        bool IsDead { get; }
        bool HasCheckpoint { get; }
        string RestoreError { get; }
        IDisposable AcquireInput();
        IDisposable AcquireMotion();
        void CloseOtherWindows();
        void RestoreOtherWindows();
        void BeginRetry(bool fromCheckpoint, Action<CommandResult> completed);
        CommandResult Quit();
    }
}
