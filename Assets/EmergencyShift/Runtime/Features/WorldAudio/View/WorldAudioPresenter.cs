using System;
using EmergencyShift.Features.WorldAudio.Config;
using EmergencyShift.Features.WorldAudio.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.WorldAudio.View
{
    /// <summary>Runtime-only player presentation, composed by the level. Owns no input or time/audio pause.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldAudioPresenter : MonoBehaviour, IDisposable
    {
        private PlayerLife2D player;
        private WorldAudioBus bus;
        private WorldAudioSettings settings;
        private WorldAudioController controller;
        private bool started;
        private bool disposed;

        public float Gain => controller?.Gain ?? 1;

        [Inject]
        private void Construct(PlayerLife2D player, WorldAudioBus bus, WorldAudioSettings settings)
        {
            this.player = player;
            this.bus = bus;
            this.settings = settings;
        }

        private void Start()
        {
            started = true;
            Attach();
        }

        private void OnEnable()
        {
            if (started)
            {
                Attach();
            }
        }

        private void LateUpdate() => controller?.Step(Time.unscaledDeltaTime);

        private void OnDisable() => Detach();

        public void Dispose()
        {
            disposed = true;
            Detach();
        }

        private void Detach()
        {
            if (player != null)
            {
                player.HealthChanged -= OnHealthChanged;
            }

            controller?.Dispose();
            controller = null;
        }

        private void Attach()
        {
            if (controller != null || disposed)
            {
                return;
            }

            controller = new WorldAudioController(new WorldAudioModel(settings), bus.AcquireGain());
            player.HealthChanged += OnHealthChanged;
            OnHealthChanged(player.Health);
        }

        private void OnHealthChanged(int current) => controller.SetDefeated(current <= 0);
    }
}
