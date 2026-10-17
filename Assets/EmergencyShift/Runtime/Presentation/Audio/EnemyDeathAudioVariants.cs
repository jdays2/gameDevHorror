using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Session-owned last choice per bank; shared assets never hold playback state.</summary>
    public static class EnemyDeathAudioVariants
    {
        private static readonly Dictionary<EnemyDeathAudioSet, AudioClip> lastPlayed = new Dictionary<EnemyDeathAudioSet, AudioClip>();
        private static readonly System.Random random = new System.Random();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            lastPlayed.Clear();
        }

        public static AudioClip Choose(EnemyDeathAudioSet sounds)
        {
            if (sounds == null || sounds.Clips == null)
            {
                return null;
            }

            lastPlayed.TryGetValue(sounds, out AudioClip previous);
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

            int selection = random.Next(alternatives);
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
    }
}
