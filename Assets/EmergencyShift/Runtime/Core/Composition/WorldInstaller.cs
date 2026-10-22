using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.CheckpointSession.Logic;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood.Config;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Core.Composition
{
    /// <summary>One world lifetime across scene reloads and additive level contexts.</summary>
    public sealed class WorldInstaller : MonoInstaller
    {
        [SerializeField]
        private AudioThreatSignalProfile threatSignals;

        public override void InstallBindings()
        {
            if (threatSignals == null)
            {
                throw new InvalidOperationException("World composition requires an audio threat signal profile.");
            }

            AudioThreatSignalSettings signalSettings = threatSignals.ToSettings();
            Container.Bind<AudioThreatSignalSettings>().FromInstance(signalSettings);
            Container.Bind<IWorldTimeScale>().To<UnityWorldTimeScale>().AsSingle();
            Container.Bind<IFrameClock>().To<UnityFrameClock>().AsSingle();
            Container.Bind<NavigationWorkBudget2D>().AsSingle();
            Container.Bind<GoreSpawnBudget2D>().AsSingle();
            Container.BindInterfacesAndSelfTo<CheckpointPathRegistry>().AsSingle();
            Container.Bind<RandomSourceFactory>().AsSingle().WithArguments(new System.Random());
            Container.BindInterfacesAndSelfTo<WorldMotionPause>().AsSingle();
            Container.BindInterfacesAndSelfTo<EnemyDeathAudioVariants>().AsSingle()
                .WithArguments(new SystemRandomSource(new System.Random()));
        }
    }
}
