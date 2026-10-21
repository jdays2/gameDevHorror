using System;
using System.Collections.Generic;
using EmergencyShift.Core.Randomness;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Session-owned last choice per bank; shared assets never hold playback state.</summary>
    public sealed class EnemyDeathAudioVariants : IDisposable
    {
        private readonly Dictionary<EnemyDeathAudioSet, AudioClip> lastPlayed = new Dictionary<EnemyDeathAudioSet, AudioClip>();
        private readonly IRandomSource random;
        private bool disposed;

        public EnemyDeathAudioVariants(IRandomSource random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public AudioClip Choose(EnemyDeathAudioSet sounds)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EnemyDeathAudioVariants));
            }

            if (sounds == null || sounds.Clips.Count == 0)
            {
                return null;
            }

            lastPlayed.TryGetValue(sounds, out AudioClip previous);
            sounds.Validate();
            int alternatives = 0;
            AudioClip fallback = null;
            foreach (AudioClip clip in sounds.Clips)
            {
                if (clip == null)
                {
                    continue;
                }

                fallback = clip;
                if (clip != previous)
                {
                    alternatives++;
                }
            }

            if (alternatives == 0)
            {
                // Empty banks stay silent; a single valid recording remains usable.
                return fallback;
            }

            int selection = random.NextInt(0, alternatives);
            foreach (AudioClip clip in sounds.Clips)
            {
                if (clip == null || clip == previous)
                {
                    continue;
                }

                if (selection == 0)
                {
                    lastPlayed[sounds] = clip;
                    return clip;
                }

                selection--;
            }

            return fallback;
        }

        public void Dispose()
        {
            disposed = true;
            lastPlayed.Clear();
        }
    }
}
