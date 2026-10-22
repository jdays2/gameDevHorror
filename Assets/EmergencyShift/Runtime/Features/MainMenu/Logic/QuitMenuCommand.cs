using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class QuitMenuCommand : ICommand
    {
        private readonly MainMenuModel model;
        private readonly IMenuNavigation navigation;

        public QuitMenuCommand(MainMenuModel model, IMenuNavigation navigation)
        {
            this.model = model;
            this.navigation = navigation;
        }

        public CommandResult CanExecute() => model.Page == MenuPage.Quit && !navigation.IsBusy
            ? CommandResult.Ok() : CommandResult.Fail("Подтвердите выход из игры.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                navigation.Quit();
            }

            return result;
        }
    }
}
