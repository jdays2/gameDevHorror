using Zenject;

namespace EmergencyShift.Features.Interaction
{
    public static class PlayerInteractionInstaller
    {
        public static void Install(DiContainer container) => container.Bind<PlayerInteractionControllerFactory>().AsSingle();
    }
}
