using System;
using EmergencyShift.Features.AudioMood.Config;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared tuning and replaceable sounds; runtime perception is owned by the scene director.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Audio mood profile")]
    public sealed class AudioMoodProfile : ScriptableObject
    {
        public const float DefaultDamageDuckSeconds = .6f;
        [SerializeField]
        private AudioClip exploration;
        [SerializeField]
        private AudioClip threat;
        [SerializeField]
        private AudioClip combat;
        [SerializeField, Range(0, 1)]
        private float masterVolume = .7f;
        [SerializeField, Range(0, 1)]
        private float explorationGain = .6f;
        [SerializeField, Range(0, 1)]
        private float threatGain = .85f;
        [SerializeField, Range(0, 1)]
        private float combatGain = .9f;
        [SerializeField, Min(.05f)]
        private float riseSeconds = .45f;
        [SerializeField, Min(.05f)]
        private float fallSeconds = 3.5f;
        [SerializeField, Min(.1f)]
        private float threatHoldSeconds = 5f;
        [SerializeField, Min(.1f)]
        private float combatHoldSeconds = 3f;
        [SerializeField, Min(.5f)]
        private float awarenessDistance = 9f;
        [Tooltip("One perceived pursuer within this distance already creates combat pressure.")]
        [SerializeField, Min(.5f)]
        private float closeCombatDistance = 2.5f;
        [UnityEngine.Serialization.FormerlySerializedAs("closeEnemiesForCombat")]
        [Tooltip("Perceived engaged enemies within awareness distance; they need not all be next to the player.")]
        [SerializeField, Min(1)]
        private int crowdEnemiesForCombat = 2;
        [SerializeField, Min(.05f)]
        private float observationInterval = .12f;
        [SerializeField, Min(0)]
        private float perceptionMemorySeconds = 1.25f;
        [SerializeField, Range(0, 1)]
        private float duckGain = .75f;
        [SerializeField, Min(.05f)]
        private float shotDuckSeconds = .22f;
        [SerializeField, Min(.05f)]
        private float cueDuckSeconds = .4f;
        [SerializeField, Range(0, 1)]
        private float lowHealthGain = .9f;
        [SerializeField, Min(.05f)]
        private float damageDuckSeconds = DefaultDamageDuckSeconds;
        [SerializeField, Min(.05f)]
        private float combatAfterShotSeconds = 2;
        [SerializeField, Min(.05f)]
        private float deathFadeSeconds = .55f;
        [SerializeField, Min(.02f)]
        private float duckResponseSeconds = .06f;
        [SerializeField, Min(.01f)]
        private float scheduleLeadSeconds = .15f;
        [SerializeField, Range(0, 256)]
        private int layerPriority = 120;

        public AudioClip Exploration => exploration;
        public AudioClip Threat => threat;
        public AudioClip Combat => combat;
        public float MasterVolume => masterVolume;
        public float ExplorationGain => explorationGain;
        public float ThreatGain => threatGain;
        public float CombatGain => combatGain;
        public float RiseSeconds => riseSeconds;
        public float FallSeconds => fallSeconds;
        public float ThreatHoldSeconds => threatHoldSeconds;
        public float CombatHoldSeconds => combatHoldSeconds;
        public float AwarenessDistance => awarenessDistance;
        public float CloseCombatDistance => closeCombatDistance;
        public int CrowdEnemiesForCombat => crowdEnemiesForCombat;
        public float ObservationInterval => observationInterval;
        public float PerceptionMemorySeconds => perceptionMemorySeconds;
        public float DuckGain => duckGain;
        public float ShotDuckSeconds => shotDuckSeconds;
        public float CueDuckSeconds => cueDuckSeconds;
        public float LowHealthGain => lowHealthGain;
        public float DamageDuckSeconds => damageDuckSeconds;
        public float CombatAfterShotSeconds => combatAfterShotSeconds;
        public float DeathFadeSeconds => deathFadeSeconds;
        public float DuckResponseSeconds => duckResponseSeconds;
        public float ScheduleLeadSeconds => scheduleLeadSeconds;
        public int LayerPriority => layerPriority;

        public AudioMoodSettings ToMoodSettings()
        {
            Validate();
            return new AudioMoodSettings(observationInterval, perceptionMemorySeconds, awarenessDistance,
                closeCombatDistance, crowdEnemiesForCombat, threatHoldSeconds, combatHoldSeconds,
                combatAfterShotSeconds, shotDuckSeconds, damageDuckSeconds, cueDuckSeconds);
        }

        public AudioLayerMixSettings ToMixSettings()
        {
            Validate();
            return new AudioLayerMixSettings(masterVolume, new Vector3(explorationGain, threatGain, combatGain),
                riseSeconds, fallSeconds, deathFadeSeconds, duckGain, duckResponseSeconds, lowHealthGain);
        }

        public void Validate()
        {
            if (!Unit(masterVolume) || !Unit(explorationGain) || !Unit(threatGain) || !Unit(combatGain) ||
                !Unit(duckGain) || !Unit(lowHealthGain) ||
                !AtLeast(riseSeconds, .05f) || !AtLeast(fallSeconds, .05f) ||
                !AtLeast(threatHoldSeconds, .1f) || !AtLeast(combatHoldSeconds, .1f) ||
                !AtLeast(awarenessDistance, .5f) || !AtLeast(closeCombatDistance, .5f) || closeCombatDistance > awarenessDistance ||
                crowdEnemiesForCombat < 1 || !AtLeast(observationInterval, .05f) || !AtLeast(perceptionMemorySeconds, 0) ||
                !AtLeast(shotDuckSeconds, .05f) || !AtLeast(cueDuckSeconds, .05f) ||
                !AtLeast(damageDuckSeconds, .05f) || !AtLeast(combatAfterShotSeconds, .05f) ||
                !AtLeast(deathFadeSeconds, .05f) || !AtLeast(duckResponseSeconds, .02f) ||
                !AtLeast(scheduleLeadSeconds, .01f) || layerPriority < 0 || layerPriority > 256)
            {
                throw new InvalidOperationException("Invalid audio mood profile: " + name);
            }
        }

        private static bool Unit(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
        private static bool AtLeast(float value, float minimum) => float.IsFinite(value) && value >= minimum;
    }
}
