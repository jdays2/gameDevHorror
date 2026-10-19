using EmergencyShift.Core.Composition;
using Zenject;

namespace EmergencyShift
{
    public sealed class LevelRegistriesInstaller : Installer<LevelRegistriesInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SceneRegistry<Door2D>>().AsSingle();
            Container.BindInterfacesAndSelfTo<SceneRegistry<CrowdAgent2D>>().AsSingle();
            Container.BindInterfacesAndSelfTo<SceneRegistry<StalkerAudio2D>>().AsSingle();
            Container.BindInterfacesAndSelfTo<SceneRegistry<IGroundAudioRegion>>().AsSingle();
            Container.Bind<GroundAudioSurfaceQuery2D>().AsSingle();
            Container.Bind<EnemyChasePathFactory>().AsSingle();
        }
    }
}
