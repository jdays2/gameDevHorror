using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Immutable rupture recordings shared by species audio adapters.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Audio/Enemy death sounds")]
    public sealed class EnemyDeathAudioSet : ScriptableObject
    {
        [SerializeField]
        private AudioClip[] clips = System.Array.Empty<AudioClip>();
        [SerializeField, Range(0, 1), Tooltip("Shared rupture volume multiplier; other creature voices are unaffected.")]
        private float volume = 1;
        public IReadOnlyList<AudioClip> Clips => clips;
        public float Volume => Mathf.Clamp01(volume);
    }
}
