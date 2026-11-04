using System;
using EmergencyShift.Features.PlayerHitAudio.Config;
using EmergencyShift.Features.PlayerHitAudio.View;
using EmergencyShift.Features.WorldAudio.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.PlayerHitAudio
{
    public sealed class PlayerHitAudioInstaller : MonoInstaller
    {
        [SerializeField] private PlayerHitAudioProfile profile;

        public override void InstallBindings()
        {
            if (profile == null)
            {
                throw new InvalidOperationException("Player hit audio requires its authored profile.");
            }

            profile.Validate();
            Container.Bind<PlayerHitAudioProfile>().FromInstance(profile);
        }

        public static PlayerHitAudioPresenter AttachPlayer(DiContainer container, PlayerLife2D player)
        {
            if (!Application.isPlaying || player == null ||
                !container.HasBinding<PlayerHitAudioProfile>() || !container.HasBinding<WorldAudioBus>())
            {
                return null;
            }
            if (player.GetComponent<PlayerHitAudioPresenter>() != null)
            {
                throw new InvalidOperationException("One player scope owns one hit audio presenter.");
            }

            return container.InstantiateComponent<PlayerHitAudioPresenter>(player.gameObject);
        }
    }
}
