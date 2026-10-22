using System;
using EmergencyShift.Features.MainMenu.Config;
using EmergencyShift.Features.MainMenu.Logic;
using EmergencyShift.Features.MainMenu.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.MainMenu
{
    public sealed class MainMenuWorldInstaller : MonoInstaller
    {
        [SerializeField] private MainMenuCatalog catalog;
        [SerializeField] private MenuNavigationAdapter navigation;

        public override void InstallBindings()
        {
            if (catalog == null || navigation == null)
            {
                throw new InvalidOperationException("Main menu needs its authored catalog and navigation adapter.");
            }

            Container.Bind<MenuSceneCatalog>().FromInstance(catalog.ToCatalog());
            Container.Bind<IMenuNavigation>().FromInstance(navigation);
            Container.Bind<IMenuPreferencesPlatform>().To<UnityMenuPreferences>().AsSingle();
            Container.Bind<MenuPreferencesController>().AsSingle().NonLazy();
        }
    }
}
