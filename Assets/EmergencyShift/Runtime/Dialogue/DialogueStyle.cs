using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Dialogue/Style")]
    public sealed class DialogueStyle : ScriptableObject
    {
        [SerializeField]
        private Font font;
        [SerializeField]
        private Color textColor = new Color(.949f, .929f, .894f);
        [SerializeField]
        private Color accentColor = new Color(.808f, .624f, .475f);
        [SerializeField]
        private Color hintColor = new Color(.725f, .706f, .675f);
        [Min(20)]
        [SerializeField]
        private int textSize = 40;
        [Min(16)]
        [SerializeField]
        private int nameSize = 24;
        [Min(16)]
        [SerializeField]
        private int hintSize = 20;
        [Min(64)]
        [SerializeField]
        private float portraitSize = 300;
        [Min(0)]
        [SerializeField]
        private float portraitGap = 48;
        [Min(0)]
        [SerializeField]
        private float bottomMargin = 48;
        [Min(0)]
        [SerializeField]
        private float nameHeight = 36;
        [Min(0)]
        [SerializeField]
        private float textGap = 24;
        [Min(44)]
        [SerializeField]
        private float footerHeight = 64;
        [Min(20)]
        [SerializeField]
        private int choiceSize = 30;
        [Min(44)]
        [SerializeField]
        private float choiceMinHeight = 64;
        [Min(0)]
        [SerializeField]
        private float choiceGap = 12;
        [Min(0)]
        [SerializeField]
        private float choicePadding = 16;
        [SerializeField]
        private Color choiceColor = new Color(.11f, .12f, .14f, .65f);
        [SerializeField]
        private Color shadowColor = new Color(.015f, .022f, .035f, .97f);
        [Min(1)]
        [SerializeField]
        private float shadowExtraHeight = 360;

        public Font Font => font;
        public Color TextColor => textColor;
        public Color AccentColor => accentColor;
        public Color HintColor => hintColor;
        public int TextSize => textSize;
        public int NameSize => nameSize;
        public int HintSize => hintSize;
        public float PortraitSize => portraitSize;
        public float PortraitGap => portraitGap;
        public float BottomMargin => bottomMargin;
        public float NameHeight => nameHeight;
        public float TextGap => textGap;
        public float FooterHeight => footerHeight;
        public int ChoiceSize => choiceSize;
        public float ChoiceMinHeight => choiceMinHeight;
        public float ChoiceGap => choiceGap;
        public float ChoicePadding => choicePadding;
        public Color ChoiceColor => choiceColor;
        public Color ShadowColor => shadowColor;
        public float ShadowExtraHeight => shadowExtraHeight;

        public void Validate()
        {
            if (!ValidColor(textColor) || !ValidColor(accentColor) || !ValidColor(hintColor) ||
                !ValidColor(choiceColor) || !ValidColor(shadowColor) ||
                textSize < 20 || nameSize < 16 || hintSize < 16 || choiceSize < 20 ||
                !AtLeast(portraitSize, 64) || !AtLeast(portraitGap, 0) || !AtLeast(bottomMargin, 0) ||
                !AtLeast(nameHeight, 0) || !AtLeast(textGap, 0) || !AtLeast(footerHeight, 44) ||
                !AtLeast(choiceMinHeight, 44) || !AtLeast(choiceGap, 0) || !AtLeast(choicePadding, 0) ||
                !AtLeast(shadowExtraHeight, 1))
            {
                throw new InvalidOperationException("Invalid dialogue style: " + name);
            }
        }

        private static bool AtLeast(float value, float minimum) => float.IsFinite(value) && value >= minimum;

        private static bool ValidColor(Color value) => float.IsFinite(value.r) && float.IsFinite(value.g) &&
            float.IsFinite(value.b) && float.IsFinite(value.a) && value.a >= 0 && value.a <= 1;
    }
}
