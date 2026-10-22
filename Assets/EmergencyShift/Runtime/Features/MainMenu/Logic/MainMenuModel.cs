using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class MainMenuModel
    {
        private MenuPage previousPage;
        public MenuSceneCatalog Catalog { get; }
        public bool OwnerAccess { get; }
        public MenuPage Page { get; private set; }
        public string Error { get; private set; }
        public event Action Changed;

        public MainMenuModel(MenuSceneCatalog catalog, bool ownerAccess)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            OwnerAccess = ownerAccess;
        }

        public CommandResult CanOpen(MenuPage page)
        {
            if (!Enum.IsDefined(typeof(MenuPage), page) || page == MenuPage.Loading)
            {
                return CommandResult.Fail("Неизвестный раздел меню.");
            }

            if (Page == MenuPage.Loading)
            {
                return CommandResult.Fail("Дождитесь окончания перехода.");
            }

            if (page == MenuPage.Scenes && !OwnerAccess)
            {
                return CommandResult.Fail("Служебное меню доступно только в среде разработки.");
            }

            return CommandResult.Ok();
        }

        public CommandResult CanLaunch(string id)
        {
            MenuScene scene = Catalog.Find(id);
            bool permittedPage = Page == MenuPage.NewGame || Page == MenuPage.Scenes;
            if (!permittedPage || scene == null)
            {
                return CommandResult.Fail("Выберите доступную сцену.");
            }

            if ((Page == MenuPage.Scenes && !OwnerAccess) ||
                (Page == MenuPage.NewGame && id != Catalog.NewGameId))
            {
                return CommandResult.Fail("Этот маршрут не доступен из текущего раздела.");
            }

            return CommandResult.Ok();
        }

        internal void Open(MenuPage page)
        {
            Page = page;
            Error = null;
            Changed?.Invoke();
        }

        internal void BeginLoad()
        {
            previousPage = Page;
            Page = MenuPage.Loading;
            Error = null;
            Changed?.Invoke();
        }

        internal void RejectLoad(string reason)
        {
            if (Page == MenuPage.Loading)
            {
                Page = previousPage;
            }

            Error = reason;
            Changed?.Invoke();
        }
    }
}
