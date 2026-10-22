using EmergencyShift.Core.Composition;
using EmergencyShift.Features.Flashlight.Logic;
using Zenject;

namespace EmergencyShift.Features.Flashlight
{
    public static class FlashlightInstaller
    {
        public static void Install(DiContainer container, Flashlight2D sceneLight)
        {
            container.Bind<FlashlightControllerFactory>().AsSingle();
            container.Bind<IFlashlightOpticsSource>().FromInstance(sceneLight);
            var camera = new CameraWorldProjection2D(sceneLight != null ? sceneLight.WorldCamera : null);
            container.Bind<IWorldProjection2D>().FromInstance(camera);
            container.Bind<ICameraImpactView>().FromInstance(camera);
        }
    }
}
