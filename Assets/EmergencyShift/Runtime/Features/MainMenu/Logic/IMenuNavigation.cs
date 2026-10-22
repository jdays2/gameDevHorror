using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public interface IMenuNavigation
    {
        bool OwnerAccess { get; }
        bool IsBusy { get; }
        event Action<string> LoadFailed;
        event Action OwnerMenuRequested;
        bool CanLoad(string scenePath);
        CommandResult Load(string scenePath);
        void Quit();
    }
}
