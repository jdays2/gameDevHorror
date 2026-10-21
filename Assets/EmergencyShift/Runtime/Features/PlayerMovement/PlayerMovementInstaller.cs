using Zenject;

namespace EmergencyShift.Features.PlayerMovement
{
    public static class PlayerMovementInstaller
    {
        public static void Install(DiContainer container) => container.Bind<PlayerMovementControllerFactory>().AsSingle();
    }
}
