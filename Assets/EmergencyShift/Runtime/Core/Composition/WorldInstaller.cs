using EmergencyShift.Core.Time;
using Zenject;

namespace EmergencyShift.Core.Composition
{
    /// <summary>One world lifetime across scene reloads and additive level contexts.</summary>
    public sealed class WorldInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IWorldTimeScale>().To<UnityWorldTimeScale>().AsSingle();
            Container.BindInterfacesAndSelfTo<WorldMotionPause>().AsSingle();
        }
    }
}
