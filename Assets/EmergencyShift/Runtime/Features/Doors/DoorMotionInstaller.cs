using Zenject;

namespace EmergencyShift.Features.Doors
{
    public static class DoorMotionInstaller
    {
        public static void Install(DiContainer container) => container.Bind<DoorMotionControllerFactory>().AsSingle();
    }
}
