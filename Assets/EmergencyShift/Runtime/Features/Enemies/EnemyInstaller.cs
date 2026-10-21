using Zenject;

namespace EmergencyShift.Features.Enemies
{
    public static class EnemyInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<EnemyMeleeControllerFactory>().AsSingle();
        }
    }
}
