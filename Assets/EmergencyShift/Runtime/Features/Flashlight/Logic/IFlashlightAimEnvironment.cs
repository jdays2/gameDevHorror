using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public interface IFlashlightAimEnvironment
    {
        bool IsAvailable { get; }
        Vector2 OwnerPosition { get; }
        float Angle { get; }
        void SetAngle(float angle);
    }
}
