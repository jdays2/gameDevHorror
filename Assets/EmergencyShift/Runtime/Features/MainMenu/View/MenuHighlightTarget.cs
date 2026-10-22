using EmergencyShift.Features.MainMenu.Config;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmergencyShift.Features.MainMenu.View
{
    public sealed class MenuHighlightTarget : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private MainMenuStyle style;
        [SerializeField] private UnityEngine.UI.Text label;
        [SerializeField] private UnityEngine.UI.Selectable control;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (control != null && control.IsInteractable() && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        public void OnSelect(BaseEventData eventData) => SetHighlighted(true);
        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false);

        public void SetHighlighted(bool selected)
        {
            if (label != null)
            {
                label.color = selected ? style.SelectedText : style.NormalText;
            }
        }

    }
}
