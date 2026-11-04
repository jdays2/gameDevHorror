using EmergencyShift.Features.Pickups.View;
using Zenject;

namespace EmergencyShift.Features.Pickups
{
    /// <summary>Level-owned factories; the shared presentation profile belongs to ProjectContext.</summary>
    public static class PickupInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<PickupGlintFactory>().AsSingle();
            container.Bind<PickupControllerFactory>().AsSingle();
        }
    }
}
