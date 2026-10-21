using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public interface IFlashlightView
    {
        bool HasBeam { get; }
        void RenderPower(bool on);
        void RenderOptics(Vector2 optics);
        void OwnershipChanged();
        void PowerChanged(bool on);
        void OpticsChanged();
    }
}
