using System;
using EmergencyShift.Features.WorldAudio.Config;
using EmergencyShift.Features.WorldAudio.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.WorldAudio
{
    public sealed class WorldAudioInstaller : MonoInstaller
    {
        [SerializeField] private WorldAudioProfile profile;

        public override void InstallBindings()
        {
            if (profile == null)
            {
                throw new InvalidOperationException("World audio needs its authored transition profile.");
            }

            profile.Validate();
            Container.Bind<WorldAudioSettings>().FromInstance(profile.ToSettings());
            Container.BindInterfacesAndSelfTo<WorldAudioBus>().AsSingle().WithArguments(profile);
        }

        public static WorldAudioPresenter AttachPlayer(DiContainer container, PlayerLife2D player)
        {
            // Isolated EditMode containers can deliberately omit this presentation feature.
            if (Application.isPlaying && player != null && container.HasBinding<WorldAudioBus>())
            {
                return container.InstantiateComponent<WorldAudioPresenter>(player.gameObject);
            }

            return null;
        }
    }
}
