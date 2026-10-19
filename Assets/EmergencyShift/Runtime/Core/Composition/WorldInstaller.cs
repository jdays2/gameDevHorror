using EmergencyShift.Core.Randomness;
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
            Container.Bind<IFrameClock>().To<UnityFrameClock>().AsSingle();
            Container.Bind<NavigationWorkBudget2D>().AsSingle();
            Container.Bind<GoreSpawnBudget2D>().AsSingle();
            Container.Bind<RandomSourceFactory>().AsSingle().WithArguments(new System.Random());
            Container.BindInterfacesAndSelfTo<WorldMotionPause>().AsSingle();
            Container.BindInterfacesAndSelfTo<EnemyDeathAudioVariants>().AsSingle()
                .WithArguments(new SystemRandomSource(new System.Random()));
        }
    }
}
