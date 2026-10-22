using System;
using UnityEngine;

namespace EmergencyShift.Features.MainMenu.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/UI/Main menu style")]
    public sealed class MainMenuStyle : ScriptableObject
    {
        [SerializeField] private Color normalText = new Color(0.62f, 0.62f, 0.62f);
        [SerializeField] private Color selectedText = new Color(0.93f, 0.93f, 0.93f);
        public Color NormalText => normalText;
        public Color SelectedText => selectedText;

        public void Validate()
        {
            ValidateColor(normalText);
            ValidateColor(selectedText);
        }

        private static void ValidateColor(Color value)
        {
            if (!float.IsFinite(value.r) || !float.IsFinite(value.g) || !float.IsFinite(value.b) ||
                !float.IsFinite(value.a) || value.a < 0 || value.a > 1)
            {
                throw new InvalidOperationException("Invalid menu color.");
            }
        }
    }
}
