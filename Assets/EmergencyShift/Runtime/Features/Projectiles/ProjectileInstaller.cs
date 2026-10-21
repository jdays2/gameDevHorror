using Zenject;

namespace EmergencyShift.Features.Projectiles
{
    public static class ProjectileInstaller
    {
        public static void Install(DiContainer container) => container.Bind<ProjectileFlightControllerFactory>().AsSingle();
    }
}
