using Zenject;

namespace EmergencyShift.Features.Pickups
{
    public static class PickupInstaller
    {
        public static void Install(DiContainer container) => container.Bind<PickupControllerFactory>().AsSingle();
    }
}
