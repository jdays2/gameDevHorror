using Zenject;

namespace EmergencyShift.Features.PlayerFootsteps
{
    public static class PlayerFootstepInstaller
    {
        public static void Install(DiContainer container) => container.Bind<PlayerFootstepControllerFactory>().AsSingle();
    }
}
