using System;
using UnityEngine;

namespace EmergencyShift
{
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] private DialogueStyle style;
        [SerializeField] private Canvas overlay;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform backdrop;
        [SerializeField] private UnityEngine.UI.Image portrait;
        [SerializeField] private UnityEngine.UI.Text speakerName;
        [SerializeField] private UnityEngine.UI.Text body;
        [SerializeField] private UnityEngine.UI.Button advance;
        [SerializeField] private UnityEngine.UI.Text advanceLabel;
        private bool reflowing;

        public event Action AdvanceRequested;
        public bool IsVisible => overlay != null && overlay.enabled;
        public string DisplayedSpeaker => speakerName.text;
        public string DisplayedText => body.text;
        public UnityEngine.UI.Button AdvanceButton => advance;

        private void Awake()
        {
            advance.onClick.AddListener(OnAdvance);
            Hide();
        }

        private void OnDestroy()
        {
            if (advance != null)
            {
                advance.onClick.RemoveListener(OnAdvance);
            }
        }

        private void OnAdvance()
        {
            AdvanceRequested?.Invoke();
        }

        public void Present(DialogueSequence.Speaker speaker, string text, bool last)
        {
            speakerName.font = style.font;
            speakerName.fontSize = style.nameSize;
            speakerName.color = style.accentColor;
            speakerName.text = speaker.displayName.ToUpperInvariant();
            body.font = style.font;
            body.fontSize = style.textSize;
            body.color = style.textColor;
            body.text = text;
            advanceLabel.font = style.font;
            advanceLabel.fontSize = style.hintSize;
            advanceLabel.color = style.hintColor;
            advanceLabel.text = last ? "Закончить разговор   [Space / Enter]   →" : "Продолжить   [Space / Enter]   →";
            portrait.sprite = speaker.portrait;
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            overlay.enabled = true;
            Canvas.ForceUpdateCanvases();
            Reflow();
        }

        public void Hide()
        {
            overlay.enabled = false;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (IsVisible)
            {
                Reflow();
            }
        }

        private void Reflow()
        {
            if (reflowing || panel == null || body == null || style == null)
            {
                return;
            }

            reflowing = true;
            float textHeight = Mathf.Max(style.textSize * 1.5f, body.preferredHeight);
            float height = style.nameHeight + style.textGap + textHeight + style.textGap + style.footerHeight;
            panel.sizeDelta = new Vector2(0, height);
            panel.anchoredPosition = new Vector2(0, style.bottomMargin);
            speakerName.rectTransform.sizeDelta = new Vector2(0, style.nameHeight);
            body.rectTransform.anchoredPosition = new Vector2(0, -style.nameHeight - style.textGap);
            body.rectTransform.sizeDelta = new Vector2(0, textHeight);
            advance.GetComponent<RectTransform>().sizeDelta = new Vector2(650, style.footerHeight);
            portrait.rectTransform.sizeDelta = Vector2.one * style.portraitSize;
            portrait.rectTransform.anchoredPosition = new Vector2(0, style.bottomMargin + height + style.portraitGap);
            backdrop.sizeDelta = new Vector2(0, style.bottomMargin + height + style.textGap);
            reflowing = false;
        }
    }
}
