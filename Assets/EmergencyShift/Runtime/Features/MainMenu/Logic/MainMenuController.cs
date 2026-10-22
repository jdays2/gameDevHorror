using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class MainMenuController : IDisposable
    {
        private readonly IMenuNavigation navigation;
        private bool disposed;
        public MainMenuModel Model { get; }

        public MainMenuController(MainMenuModel model, IMenuNavigation navigation)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            navigation.LoadFailed += OnLoadFailed;
        }

        public CommandResult Open(MenuPage page) => disposed ? Closed() : new OpenMenuPageCommand(Model, page).Execute();
        public CommandResult Launch(string sceneId) => disposed ? Closed() : new LaunchMenuSceneCommand(Model, navigation, sceneId).Execute();
        public CommandResult Quit() => disposed ? Closed() : new QuitMenuCommand(Model, navigation).Execute();

        public void Dispose()
        {
            if (!disposed)
            {
                navigation.LoadFailed -= OnLoadFailed;
                disposed = true;
            }
        }

        private void OnLoadFailed(string reason) => Model.RejectLoad(reason);
        private static CommandResult Closed() => CommandResult.Fail("Контекст меню закрыт.");
    }
}
