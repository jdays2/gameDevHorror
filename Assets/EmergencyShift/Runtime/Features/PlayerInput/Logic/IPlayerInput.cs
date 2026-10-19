using UnityEngine;

namespace EmergencyShift.Features.PlayerInput.Logic
{
    /// <summary>Input intent only. Implementations cannot authorize simulation changes.</summary>
    public interface IPlayerInput
    {
        Vector2 Movement { get; }
        Vector2 PointerPosition { get; }
        bool HasPointer { get; }
        bool FirePressed { get; }
        bool FireHeld { get; }
        bool InteractPressed { get; }
        bool ReloadPressed { get; }
        bool SelectPistolPressed { get; }
        bool SelectShotgunPressed { get; }
        bool ToggleLightPressed { get; }
        bool RestartPressed { get; }
        bool RefillAmmoPressed { get; }
        bool DialogueAdvancePressed { get; }
        bool ConfirmationHeld { get; }
    }
}
