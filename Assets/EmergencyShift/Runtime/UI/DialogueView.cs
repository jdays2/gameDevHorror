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
        private bool reflowing;
        private int choiceCount;

        public event Action AdvanceRequested;
        public event Action<int> ChoiceRequested;
        public bool IsVisible => overlay != null && overlay.enabled;
        public string DisplayedSpeaker => speakerName.text;
        public string DisplayedText => body.text;
        public UnityEngine.UI.Button AdvanceButton => advance;
        public IReadOnlyList<UnityEngine.UI.Button> ChoiceButtons => choiceButtons;

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
        }

        private void OnAdvance()
        {
            AdvanceRequested?.Invoke();
        }

        public void Present(DialogueSequence.Speaker speaker, DialogueNode line, bool last)
        {
            speakerName.font = style.font;
            speakerName.fontSize = style.nameSize;
            speakerName.color = style.accentColor;
            speakerName.text = speaker.displayName.ToUpperInvariant();
            body.font = style.font;
            body.fontSize = style.textSize;
            body.color = style.textColor;
            body.text = line.Text;
            choiceCount = line.Choices.Count;
            PrepareChoices(line.Choices);
            advance.gameObject.SetActive(choiceCount == 0);
            backgroundAdvance.interactable = choiceCount == 0;
            advanceLabel.font = style.font;
            advanceLabel.fontSize = style.hintSize;
            advanceLabel.color = style.hintColor;
            advanceLabel.text = last ? "Закончить разговор   [ЛКМ / Space / Enter]   →" : "Продолжить   [ЛКМ / Space / Enter]   →";
            portrait.sprite = speaker.portrait;
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            overlay.enabled = true;
            backdrop.GetComponentInChildren<DialogueFadeGraphic>().color = style.shadowColor;
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
                button.onClick.AddListener(() => ChoiceRequested?.Invoke(index));
                choiceButtons.Add(button);
                choiceLabels.Add(button.GetComponentInChildren<UnityEngine.UI.Text>(true));
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
                label.font = style.font;
                label.fontSize = style.choiceSize;
                label.color = style.textColor;
                label.text = (i + 1) + "   " + choices[i].Text;
                choiceButtons[i].targetGraphic.color = style.choiceColor;
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

            reflowing = true;
            float footerHeight = style.footerHeight;
            if (choiceCount > 0)
            {
                footerHeight = 0;
                for (int i = 0; i < choiceCount; i++)
                {
                    var rect = choiceButtons[i].GetComponent<RectTransform>();
                    choiceLabels[i].rectTransform.offsetMin = new Vector2(style.choicePadding, style.choicePadding);
                    choiceLabels[i].rectTransform.offsetMax = new Vector2(-style.choicePadding, -style.choicePadding);
                    float rowHeight = Mathf.Max(style.choiceMinHeight, choiceLabels[i].preferredHeight + style.choicePadding * 2);
                    rect.anchoredPosition = new Vector2(0, -footerHeight);
                    rect.sizeDelta = new Vector2(0, rowHeight);
                    footerHeight += rowHeight + style.choiceGap;
                }

                footerHeight -= style.choiceGap;
            }

            choicesRoot.sizeDelta = new Vector2(0, footerHeight);
            float textHeight = Mathf.Max(style.textSize * 1.5f, body.preferredHeight);
            float height = style.nameHeight + style.textGap + textHeight + style.textGap + footerHeight;
            panel.sizeDelta = new Vector2(0, height);
            panel.anchoredPosition = new Vector2(0, style.bottomMargin);
            speakerName.rectTransform.sizeDelta = new Vector2(0, style.nameHeight);
            body.rectTransform.anchoredPosition = new Vector2(0, -style.nameHeight - style.textGap);
            body.rectTransform.sizeDelta = new Vector2(0, textHeight);
            advance.GetComponent<RectTransform>().sizeDelta = new Vector2(800, style.footerHeight);
            portrait.rectTransform.sizeDelta = Vector2.one * style.portraitSize;
            portrait.rectTransform.anchoredPosition = new Vector2(0, style.bottomMargin + height + style.portraitGap);
            backdrop.sizeDelta = new Vector2(0, style.bottomMargin + height + style.shadowExtraHeight);
            reflowing = false;
        }
    }
}
