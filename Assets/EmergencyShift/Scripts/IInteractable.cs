using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Общий небольшой контракт двери и предмета. Ввод принадлежит игроку.</summary>
    public interface IInteractable
    {
        Vector2 InteractionPosition { get; }
        float InteractionRange { get; }
        bool IsAvailable(PlayerInteraction actor);
        string GetPrompt(PlayerInteraction actor);
        bool Interact(PlayerInteraction actor);
    }
}
