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
        private UnityEngine.UI.ScrollRect scroll;

        private void Awake() => scroll = GetComponentInParent<UnityEngine.UI.ScrollRect>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (control != null && control.IsInteractable() && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetHighlighted(true);
            if (scroll == null)
            {
                return;
            }
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
            Rect viewport = scroll.viewport.rect;
            Vector2 position = scroll.content.anchoredPosition;
            if (bounds.max.y > viewport.yMax)
            {
                position.y -= bounds.max.y - viewport.yMax;
            }
            else if (bounds.min.y < viewport.yMin)
            {
                position.y += viewport.yMin - bounds.min.y;
            }
            scroll.StopMovement();
            scroll.content.anchoredPosition = position;
        }
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
