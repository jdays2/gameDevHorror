using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        [SerializeField] private UnityEngine.UI.Button backgroundAdvance;
        [SerializeField] private RectTransform choicesRoot;
        [SerializeField] private UnityEngine.UI.Button choiceTemplate;
        private readonly List<UnityEngine.UI.Button> choiceButtons = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Text> choiceLabels = new List<UnityEngine.UI.Text>();
        private readonly List<ChoiceListener> choiceListeners = new List<ChoiceListener>();
        private System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.UI.Button> choiceButtonView;

        private readonly struct ChoiceListener
        {
            private readonly UnityEngine.Events.UnityEvent clicks;
            private readonly UnityEngine.Events.UnityAction handler;

            public ChoiceListener(UnityEngine.Events.UnityEvent clicks, UnityEngine.Events.UnityAction handler)
            {
                this.clicks = clicks;
                this.handler = handler;
            }

            public void Remove() => clicks.RemoveListener(handler);
        }
        private bool reflowing;
        private int choiceCount;

        public event Action AdvanceRequested;
        public event Action<int> ChoiceRequested;
        public bool IsVisible => overlay != null && overlay.enabled;
        public string DisplayedSpeaker => speakerName.text;
        public string DisplayedText => body.text;
        public UnityEngine.UI.Button AdvanceButton => advance;
        public IReadOnlyList<UnityEngine.UI.Button> ChoiceButtons => choiceButtonView ??= choiceButtons.AsReadOnly();

        private void Awake()
        {
            advance.onClick.AddListener(OnAdvance);
            backgroundAdvance.onClick.AddListener(OnAdvance);
            Hide();
        }

        private void OnDestroy()
        {
            if (advance != null)
            {
                advance.onClick.RemoveListener(OnAdvance);
            }

            if (backgroundAdvance != null)
            {
                backgroundAdvance.onClick.RemoveListener(OnAdvance);
            }

            foreach (ChoiceListener listener in choiceListeners)
            {
                listener.Remove();
            }

            choiceListeners.Clear();
        }

        private void OnAdvance()
        {
            AdvanceRequested?.Invoke();
        }

        public void ValidateStyle()
        {
            if (style == null)
            {
                throw new InvalidOperationException("Dialogue view requires its style.");
            }

            style.Validate();
        }

        public void Present(DialogueSequence.Speaker speaker, DialogueNode line, bool last)
        {
            ValidateStyle();
            speakerName.font = style.Font;
            speakerName.fontSize = style.NameSize;
            speakerName.color = style.AccentColor;
            speakerName.text = speaker.DisplayName.ToUpperInvariant();
            body.font = style.Font;
            body.fontSize = style.TextSize;
            body.color = style.TextColor;
            body.text = line.Text;
            choiceCount = line.Choices.Count;
            PrepareChoices(line.Choices);
            advance.gameObject.SetActive(choiceCount == 0);
            backgroundAdvance.interactable = choiceCount == 0;
            advanceLabel.font = style.Font;
            advanceLabel.fontSize = style.HintSize;
            advanceLabel.color = style.HintColor;
            advanceLabel.text = last ? "Закончить разговор   [ЛКМ / Space / Enter]   →" : "Продолжить   [ЛКМ / Space / Enter]   →";
            portrait.sprite = speaker.Portrait;
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            overlay.enabled = true;
            backdrop.GetComponentInChildren<DialogueFadeGraphic>().color = style.ShadowColor;
            Canvas.ForceUpdateCanvases();
            Reflow();
            if (EventSystem.current != null)
            {
                GameObject selection = choiceCount > 0 ? choiceButtons[0].gameObject : null;
                EventSystem.current.SetSelectedGameObject(selection);
            }
        }

        private void PrepareChoices(IReadOnlyList<DialogueOption> choices)
        {
            while (choiceButtons.Count < choiceCount)
            {
                int index = choiceButtons.Count;
                var button = Instantiate(choiceTemplate, choicesRoot);
                button.name = "Response " + (index + 1);
                UnityEngine.Events.UnityAction handler = () => ChoiceRequested?.Invoke(index);
                button.onClick.AddListener(handler);
                choiceButtons.Add(button);
                choiceLabels.Add(button.GetComponentInChildren<UnityEngine.UI.Text>(true));
                choiceListeners.Add(new ChoiceListener(button.onClick, handler));
            }

            for (int i = 0; i < choiceButtons.Count; i++)
            {
                bool active = i < choiceCount;
                choiceButtons[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var label = choiceLabels[i];
                label.font = style.Font;
                label.fontSize = style.ChoiceSize;
                label.color = style.TextColor;
                label.text = (i + 1) + "   " + choices[i].Text;
                choiceButtons[i].targetGraphic.color = style.ChoiceColor;
                int previous = (i + choiceCount - 1) % choiceCount;
                int next = (i + 1) % choiceCount;
                choiceButtons[i].navigation = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = choiceButtons[previous],
                    selectOnDown = choiceButtons[next]
                };
            }
        }

        public void Hide()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

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

            ValidateStyle();
            reflowing = true;
            float footerHeight = style.FooterHeight;
            if (choiceCount > 0)
            {
                footerHeight = 0;
                for (int i = 0; i < choiceCount; i++)
                {
                    var rect = choiceButtons[i].GetComponent<RectTransform>();
                    choiceLabels[i].rectTransform.offsetMin = new Vector2(style.ChoicePadding, style.ChoicePadding);
                    choiceLabels[i].rectTransform.offsetMax = new Vector2(-style.ChoicePadding, -style.ChoicePadding);
                    float rowHeight = Mathf.Max(style.ChoiceMinHeight, choiceLabels[i].preferredHeight + style.ChoicePadding * 2);
                    rect.anchoredPosition = new Vector2(0, -footerHeight);
                    rect.sizeDelta = new Vector2(0, rowHeight);
                    footerHeight += rowHeight + style.ChoiceGap;
                }

                footerHeight -= style.ChoiceGap;
            }

            choicesRoot.sizeDelta = new Vector2(0, footerHeight);
            float textHeight = Mathf.Max(style.TextSize * 1.5f, body.preferredHeight);
            float height = style.NameHeight + style.TextGap + textHeight + style.TextGap + footerHeight;
            panel.sizeDelta = new Vector2(0, height);
            panel.anchoredPosition = new Vector2(0, style.BottomMargin);
            speakerName.rectTransform.sizeDelta = new Vector2(0, style.NameHeight);
            body.rectTransform.anchoredPosition = new Vector2(0, -style.NameHeight - style.TextGap);
            body.rectTransform.sizeDelta = new Vector2(0, textHeight);
            advance.GetComponent<RectTransform>().sizeDelta = new Vector2(800, style.FooterHeight);
            portrait.rectTransform.sizeDelta = Vector2.one * style.PortraitSize;
            portrait.rectTransform.anchoredPosition = new Vector2(0, style.BottomMargin + height + style.PortraitGap);
            backdrop.sizeDelta = new Vector2(0, style.BottomMargin + height + style.ShadowExtraHeight);
            reflowing = false;
        }
    }
}
