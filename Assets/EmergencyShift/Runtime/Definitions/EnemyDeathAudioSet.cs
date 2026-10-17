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
        public IReadOnlyList<AudioClip> Clips => clips;
    }
}
