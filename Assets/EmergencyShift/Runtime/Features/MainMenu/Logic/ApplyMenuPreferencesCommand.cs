using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class ApplyMenuPreferencesCommand : ICommand
    {
        private readonly MenuPreferencesController owner;
        private readonly MenuPreferences values;

        public ApplyMenuPreferencesCommand(MenuPreferencesController owner, MenuPreferences values)
        {
            this.owner = owner;
            this.values = values;
        }

        public CommandResult CanExecute() => values.IsValid ? CommandResult.Ok() : CommandResult.Fail("Недопустимые настройки.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                owner.Commit(values);
            }

            return result;
        }
    }
}
