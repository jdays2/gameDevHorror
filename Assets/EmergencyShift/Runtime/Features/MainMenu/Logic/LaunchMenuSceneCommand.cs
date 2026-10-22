using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class LaunchMenuSceneCommand : ICommand
    {
        private readonly MainMenuModel model;
        private readonly IMenuNavigation navigation;
        private readonly string sceneId;

        public LaunchMenuSceneCommand(MainMenuModel model, IMenuNavigation navigation, string sceneId)
        {
            this.model = model;
            this.navigation = navigation;
            this.sceneId = sceneId;
        }

        public CommandResult CanExecute()
        {
            CommandResult result = model.CanLaunch(sceneId);
            if (!result.Success)
            {
                return result;
            }

            if (navigation.IsBusy || !navigation.CanLoad(model.Catalog.Find(sceneId).Path))
            {
                return CommandResult.Fail("Сцена недоступна в сборке или уже выполняется переход.");
            }

            return CommandResult.Ok();
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                if (model.Page != MenuPage.Loading)
                {
                    model.RejectLoad(result.Reason);
                }
                return result;
            }

            model.BeginLoad();
            result = navigation.Load(model.Catalog.Find(sceneId).Path);
            if (!result.Success)
            {
                model.RejectLoad(result.Reason);
            }

            return result;
        }
    }
}
