using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Контракт действия по E (сейчас дверь). Наземный лут использует IAutoPickup.</summary>
    public interface IInteractable
    {
        Vector2 InteractionPosition
        {
            get;
        }

        float InteractionRange
        {
            get;
        }

        bool IsAvailable(PlayerInteraction actor);
        string GetPrompt(PlayerInteraction actor);
        bool Interact(PlayerInteraction actor);
    }
}
