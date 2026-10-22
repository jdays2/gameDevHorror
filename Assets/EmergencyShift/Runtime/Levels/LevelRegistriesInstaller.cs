using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.AudioMood;
using EmergencyShift.Features.LampPower;
using Zenject;

namespace EmergencyShift
{
    public sealed class LevelRegistriesInstaller : Installer<LevelRegistriesInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<RandomSourceFactory>().WithId(RandomSourceIds.Cosmetic).AsSingle().WithArguments(new System.Random());
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
            LampPowerInstaller.Install(Container);
        }
    }
}
