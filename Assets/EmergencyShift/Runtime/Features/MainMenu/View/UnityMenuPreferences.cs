using System;
using EmergencyShift.Features.MainMenu.Logic;
using UnityEngine;

namespace EmergencyShift.Features.MainMenu.View
{
    public sealed class UnityMenuPreferences : IMenuPreferencesPlatform
    {
        private const string Key = "EmergencyShift.MenuPreferences.v1";

        public MenuPreferences Read()
        {
            var fallback = new MenuPreferences(1, Screen.fullScreen);
            if (!PlayerPrefs.HasKey(Key))
            {
                return fallback;
            }

            try
            {
                MenuPreferences saved = JsonUtility.FromJson<MenuPreferences>(PlayerPrefs.GetString(Key));
                if (saved.IsValid)
                {
                    Apply(saved);
                    return saved;
                }
            }
            catch (ArgumentException)
            {
                // An invalid preference payload never changes the user's checkpoint files.
            }

            return fallback;
        }

        public void ApplyAndSave(MenuPreferences values)
        {
            if (!values.IsValid)
            {
                throw new ArgumentException("Invalid menu preferences.", nameof(values));
            }

            Apply(values);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(values));
            PlayerPrefs.Save();
        }

        private static void Apply(MenuPreferences values)
        {
            AudioListener.volume = values.volume;
            if (!Application.isEditor)
            {
                Screen.fullScreen = values.fullscreen;
            }
        }
    }
}
