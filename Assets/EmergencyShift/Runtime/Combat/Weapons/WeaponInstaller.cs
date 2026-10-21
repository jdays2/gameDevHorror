using Zenject;

namespace EmergencyShift
{
    public static class WeaponInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<WeaponControllerFactory>().AsSingle();
        }
    }
}
