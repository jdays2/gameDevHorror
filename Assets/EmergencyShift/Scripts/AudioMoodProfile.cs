using UnityEngine;

namespace EmergencyShift
{
    public enum AudioMood { Explore, Threat, Combat }

    /// <summary>Shared tuning and replaceable sounds; runtime perception is owned by the scene director.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Audio mood profile")]
    public sealed class AudioMoodProfile : ScriptableObject
    {
        public AudioClip exploration, threat, combat;
        [Range(0, 1)] public float masterVolume = .7f;
        [Range(0, 1)] public float explorationGain = .6f;
        [Range(0, 1)] public float threatGain = .85f;
        [Range(0, 1)] public float combatGain = .9f;
        [Min(.05f)] public float riseSeconds = .45f;
        [Min(.05f)] public float fallSeconds = 3.5f;
        [Min(.1f)] public float threatHoldSeconds = 5f;
        [Min(.1f)] public float combatHoldSeconds = 3f;
        [Min(.5f)] public float awarenessDistance = 9f;
        [Tooltip("One perceived pursuer within this distance already creates combat pressure.")]
        [Min(.5f)] public float closeCombatDistance = 2.5f;
        [UnityEngine.Serialization.FormerlySerializedAs("closeEnemiesForCombat")]
        [Tooltip("Perceived engaged enemies within awareness distance; they need not all be next to the player.")]
        [Min(1)] public int crowdEnemiesForCombat = 2;
        [Min(.05f)] public float observationInterval = .12f;
        [Min(0)] public float perceptionMemorySeconds = 1.25f;
        [Range(0, 1)] public float duckGain = .75f;
        [Min(.05f)] public float shotDuckSeconds = .22f;
        [Min(.05f)] public float cueDuckSeconds = .4f;
        [Range(0, 1)] public float lowHealthGain = .9f;
    }
}
