using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class MenuPreferencesController
    {
        private readonly IMenuPreferencesPlatform platform;
        public MenuPreferences Applied { get; private set; }

        public MenuPreferencesController(IMenuPreferencesPlatform platform)
        {
            this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
            Applied = platform.Read();
            if (!Applied.IsValid)
            {
                throw new ArgumentException("The preferences platform must supply validated settings.");
            }
        }

        public CommandResult Apply(MenuPreferences values) => new ApplyMenuPreferencesCommand(this, values).Execute();

        internal void Commit(MenuPreferences values)
        {
            platform.ApplyAndSave(values);
            Applied = values;
        }
    }
}
