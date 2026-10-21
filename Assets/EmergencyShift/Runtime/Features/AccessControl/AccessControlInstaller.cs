using Zenject;

namespace EmergencyShift.Features.AccessControl
{
    public static class AccessControlInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<AccessControlFactory>().AsSingle();
            container.Bind<DoorAccessControllerFactory>().AsSingle();
        }
    }
}
