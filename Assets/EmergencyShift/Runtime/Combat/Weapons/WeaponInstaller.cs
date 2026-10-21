using Zenject;
using EmergencyShift.Features.Projectiles;

namespace EmergencyShift
{
    public static class WeaponInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<WeaponControllerFactory>().AsSingle();
            ProjectileInstaller.Install(container);
        }
    }
}
