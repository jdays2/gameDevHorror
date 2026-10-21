using EmergencyShift.Core.Composition;
using EmergencyShift.Features.AudioMood;
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
            Container.BindInterfacesAndSelfTo<SceneRegistry<LocalLamp2D>>().AsSingle();
            Container.BindInterfacesAndSelfTo<SceneRegistry<ShotEffect2D>>().AsSingle();
            Container.BindInterfacesAndSelfTo<SceneRegistry<AudioThreatEmitter2D>>().AsSingle();
            Container.Bind<LocalIlluminationQuery2D>().AsSingle();
            Container.Bind<GroundAudioSurfaceQuery2D>().AsSingle();
            Container.Bind<EnemyChasePathFactory>().AsSingle();
            AudioMoodInstaller.Install(Container);
        }
    }
}
