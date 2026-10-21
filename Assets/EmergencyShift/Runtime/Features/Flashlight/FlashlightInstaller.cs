using Zenject;

namespace EmergencyShift.Features.Flashlight
{
    public static class FlashlightInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<FlashlightControllerFactory>().AsSingle();
        }
    }
}
