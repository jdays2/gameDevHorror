using Zenject;

namespace EmergencyShift.Features.LevelProgress
{
    public static class LevelProgressInstaller
    {
        public static void Install(DiContainer container) => container.Bind<LevelProgressFactory>().AsSingle();
    }
}
