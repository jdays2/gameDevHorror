using System;
using EmergencyShift.Features.MainMenu.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.MainMenu
{
    [DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed class MainMenuContext : MonoBehaviour
    {
        private DiContainer scope;
        private MainMenuController controller;

        private void Awake()
        {
            scope = ProjectContext.Instance.Container.CreateSubContainer();
            var model = new MainMenuModel(scope.Resolve<MenuSceneCatalog>(), scope.Resolve<IMenuNavigation>().OwnerAccess);
            scope.Bind<MainMenuModel>().FromInstance(model);
            scope.Bind<MainMenuController>().AsSingle();
            controller = scope.Resolve<MainMenuController>();
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                scope.InjectGameObject(root);
            }
        }

        private void OnDestroy()
        {
            controller?.Dispose();
            scope?.UnbindAll();
            controller = null;
            scope = null;
        }
    }
}
