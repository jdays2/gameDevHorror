using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class OpenMenuPageCommand : ICommand
    {
        private readonly MainMenuModel model;
        private readonly MenuPage page;

        public OpenMenuPageCommand(MainMenuModel model, MenuPage page)
        {
            this.model = model;
            this.page = page;
        }

        public CommandResult CanExecute() => model.CanOpen(page);

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Open(page);
            }

            return result;
        }
    }
}
