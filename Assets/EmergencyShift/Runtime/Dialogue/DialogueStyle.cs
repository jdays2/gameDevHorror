using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Dialogue/Style")]
    public sealed class DialogueStyle : ScriptableObject
    {
        public Font font;
        public Color textColor = new Color(.949f, .929f, .894f);
        public Color accentColor = new Color(.808f, .624f, .475f);
        public Color hintColor = new Color(.725f, .706f, .675f);
        [Min(20)] public int textSize = 40;
        [Min(16)] public int nameSize = 24;
        [Min(16)] public int hintSize = 20;
        [Min(64)] public float portraitSize = 300;
        [Min(0)] public float portraitGap = 48;
        [Min(0)] public float bottomMargin = 48;
        [Min(0)] public float nameHeight = 36;
        [Min(0)] public float textGap = 24;
        [Min(44)] public float footerHeight = 64;
    }
}
