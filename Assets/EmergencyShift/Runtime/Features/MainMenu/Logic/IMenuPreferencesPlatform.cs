namespace EmergencyShift.Features.MainMenu.Logic
{
    public interface IMenuPreferencesPlatform
    {
        MenuPreferences Read();
        void ApplyAndSave(MenuPreferences values);
    }
}
