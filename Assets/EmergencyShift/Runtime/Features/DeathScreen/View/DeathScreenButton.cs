using UnityEngine.EventSystems;

namespace EmergencyShift.Features.DeathScreen.View
{
    /// <summary>Pointer and keyboard share the same active selection.</summary>
    public sealed class DeathScreenButton : UnityEngine.UI.Button
    {
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsActive() && IsInteractable() && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            }
        }
    }
}
