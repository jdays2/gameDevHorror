using Zenject;

namespace EmergencyShift.Features.LampPower
{
    public static class LampPowerInstaller
    {
        public static void Install(DiContainer container) => container.Bind<LampPowerControllerFactory>().AsSingle();
    }
}
