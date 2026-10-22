using System;

namespace EmergencyShift.Features.MainMenu.Logic
{
    [Serializable]
    public struct MenuPreferences
    {
        public int version;
        public float volume;
        public bool fullscreen;

        public bool IsValid => version == 1 && float.IsFinite(volume) && volume >= 0 && volume <= 1;

        public MenuPreferences(float volume, bool fullscreen)
        {
            version = 1;
            this.volume = volume;
            this.fullscreen = fullscreen;
        }
    }
}
