using System;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public interface IFlashlightOpticsSource
    {
        bool IsOwned { get; }
        bool IsOn { get; }
        float Range { get; }
        float BeamAngle { get; }
        event Action OwnershipChanged;
        event Action OpticsChanged;
        event Action<bool> PowerChanged;
    }
}
