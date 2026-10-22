using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.CheckpointSession.Logic;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Doors.Config;
using EmergencyShift.Features.LampPower.Config;
using EmergencyShift.Features.AudioMood.Config;
using EmergencyShift.Features.PlayerRules.Config;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Core.Composition
{
    /// <summary>One world lifetime across scene reloads and additive level contexts.</summary>
    public sealed class WorldInstaller : MonoInstaller
    {
        [SerializeField]
        private AudioThreatSignalProfile threatSignals;
        [SerializeField]
        private PlayerRulesProfile playerRules;

        [SerializeField] private DoorRulesProfile doorRules;
        [SerializeField] private LampFlickerProfile fixtureFlicker;

        public override void InstallBindings()
        {
            if (threatSignals == null)
            {
                throw new InvalidOperationException("World composition requires an audio threat signal profile.");
            }

            AudioThreatSignalSettings signalSettings = threatSignals.ToSettings();
            if (playerRules == null)
            {
                throw new InvalidOperationException("World composition requires its shared player rules profile.");
            }

            PlayerRulesSettings playerSettings = playerRules.ToSettings();
            if (doorRules == null || fixtureFlicker == null)
            {
                throw new InvalidOperationException("World composition requires door rules and fixture flicker profiles.");
            }

            DoorRulesSettings doorSettings = doorRules.ToSettings();
            LampFlickerSettings flickerSettings = fixtureFlicker.ToSettings();
            Container.Bind<DoorRulesSettings>().FromInstance(doorSettings);
            Container.Bind<LampFlickerSettings>().FromInstance(flickerSettings);
            Container.Bind<AudioThreatSignalSettings>().FromInstance(signalSettings);
            Container.Bind<PlayerRulesSettings>().FromInstance(playerSettings);
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
