using System;
using EmergencyShift.Features.WorldAudio.Config;

namespace EmergencyShift.Features.WorldAudio.Logic
{
    /// <summary>One death/restore envelope; callers supply presentation time, independently of world time.</summary>
    public sealed class WorldAudioModel
    {
        private readonly WorldAudioSettings settings;
        private float from = 1;
        private float elapsed;
        private float duration;
        public float Gain { get; private set; } = 1;
        public bool Defeated { get; private set; }

        public WorldAudioModel(WorldAudioSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal void SetDefeated(bool defeated)
        {
            if (Defeated == defeated)
            {
                return;
            }

            Defeated = defeated;
            from = Gain;
            elapsed = 0;
            duration = defeated ? settings.FadeOutSeconds : settings.RestoreSeconds;
        }

        public void Step(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (duration <= 0 || elapsed >= duration)
            {
                return;
            }

            elapsed = Math.Min(duration, elapsed + seconds);
            float target = Defeated ? 0 : 1;
            Gain = elapsed >= duration ? target : from + (target - from) * settings.Evaluate(elapsed / duration);
        }
    }
}
