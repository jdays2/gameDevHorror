using System;

namespace EmergencyShift.Features.AudioMood.Config
{
    public sealed class AudioMoodSettings
    {
        public float ObservationInterval { get; }
        public float MemorySeconds { get; }
        public float AwarenessDistance { get; }
        public float CloseDistance { get; }
        public int CrowdCount { get; }
        public float ThreatHold { get; }
        public float CombatHold { get; }
        public float CombatAfterShot { get; }
        public float ShotDuck { get; }
        public float DamageDuck { get; }
        public float CueDuck { get; }

        public AudioMoodSettings(float observationInterval, float memorySeconds, float awarenessDistance,
            float closeDistance, int crowdCount, float threatHold, float combatHold, float combatAfterShot,
            float shotDuck, float damageDuck, float cueDuck)
        {
            if (!AtLeast(observationInterval, .05f) || !AtLeast(memorySeconds, 0) ||
                !AtLeast(awarenessDistance, .5f) || !AtLeast(closeDistance, .5f) || closeDistance > awarenessDistance ||
                crowdCount < 1 || !AtLeast(threatHold, .1f) || !AtLeast(combatHold, .1f) ||
                !AtLeast(combatAfterShot, .05f) || !AtLeast(shotDuck, .05f) ||
                !AtLeast(damageDuck, .05f) || !AtLeast(cueDuck, .05f))
            {
                throw new ArgumentException("Invalid audio mood settings.");
            }

            ObservationInterval = observationInterval;
            MemorySeconds = memorySeconds;
            AwarenessDistance = awarenessDistance;
            CloseDistance = closeDistance;
            CrowdCount = crowdCount;
            ThreatHold = threatHold;
            CombatHold = combatHold;
            CombatAfterShot = combatAfterShot;
            ShotDuck = shotDuck;
            DamageDuck = damageDuck;
            CueDuck = cueDuck;
        }

        private static bool AtLeast(float value, float minimum) => float.IsFinite(value) && value >= minimum;
    }
}
