using System;
using EmergencyShift.Features.AudioMood.Config;
using UnityEngine;
using MoodState = EmergencyShift.AudioMood;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public sealed class AudioLayerMixModel
    {
        private readonly AudioLayerMixSettings settings;
        public Vector3 Gains { get; private set; }

        public AudioLayerMixModel(AudioLayerMixSettings settings) =>
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public void Step(float deltaTime, MoodState mood, bool alive, bool wounded, bool ducking, float volume)
        {
            if (!float.IsFinite(deltaTime) || deltaTime < 0 || !float.IsFinite(volume) ||
                mood < MoodState.Explore || mood > MoodState.Combat)
            {
                throw new ArgumentException("Invalid audio layer mix step.");
            }

            float threatGain = mood != MoodState.Explore ? settings.LayerGains.y : 0;
            float combatGain = mood == MoodState.Combat ? settings.LayerGains.z : 0;
            Vector3 target = alive ? new Vector3(settings.LayerGains.x, threatGain, combatGain) : Vector3.zero;
            float duck = alive && ducking ? settings.DuckGain : 1;
            float health = alive && wounded ? settings.LowHealthGain : 1;
            target *= settings.MasterVolume * Mathf.Clamp01(volume) * duck * health;
            float fallSeconds = alive ? settings.FallSeconds : settings.DeathFadeSeconds;
            Vector3 next = Gains;
            for (int index = 0; index < 3; index++)
            {
                float seconds = target[index] > next[index] ? settings.RiseSeconds : fallSeconds;
                if (alive && duck < 1 && target[index] < next[index])
                {
                    seconds = settings.DuckResponseSeconds;
                }

                next[index] = Mathf.Lerp(next[index], target[index], 1 - Mathf.Exp(-deltaTime / Mathf.Max(.02f, seconds)));
            }

            Gains = next;
        }

        internal void Reset() => Gains = Vector3.zero;
    }
}
