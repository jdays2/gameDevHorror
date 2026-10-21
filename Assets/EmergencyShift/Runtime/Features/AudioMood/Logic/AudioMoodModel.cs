using System;
using System.Collections.Generic;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood.Config;
using MoodState = EmergencyShift.AudioMood;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public sealed class AudioMoodModel
    {
        private readonly AudioMoodSettings settings;
        private readonly IGameClock clock;
        private readonly Dictionary<int, float> remembered = new Dictionary<int, float>();
        private readonly HashSet<int> aliveSources = new HashSet<int>();
        private readonly List<int> expired = new List<int>();
        private float nextObservation;
        private float lastThreat = float.NegativeInfinity;
        private float lastCombat = float.NegativeInfinity;
        private float lastShot = float.NegativeInfinity;
        private float duckUntil = float.NegativeInfinity;

        public MoodState Mood { get; private set; }
        public int PerceivedEnemies { get; private set; }
        public int EngagedEnemies { get; private set; }
        public int RememberedCount => remembered.Count;
        public float AwarenessDistance => settings.AwarenessDistance;
        public bool ObservationDue => Now >= nextObservation;
        public bool IsDucking => Now < duckUntil;
        private float Now
        {
            get
            {
                double value = clock.Time;
                if (!double.IsFinite(value) || value < 0 || value > float.MaxValue)
                {
                    throw new InvalidOperationException("Invalid audio mood clock.");
                }

                return (float)value;
            }
        }

        public AudioMoodModel(AudioMoodSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Observe(IReadOnlyList<AudioThreatObservation> sources, bool listenerAlive)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            float now = Now;
            nextObservation = now + settings.ObservationInterval;
            PerceivedEnemies = 0;
            EngagedEnemies = 0;
            if (!listenerAlive)
            {
                Mood = MoodState.Explore;
                return;
            }

            Prune(sources, now);
            int close = 0;
            bool attack = false;
            for (int index = 0; index < sources.Count; index++)
            {
                AudioThreatObservation source = sources[index];
                if (!source.Alive || !source.Eligible || !float.IsFinite(source.DistanceSquared) ||
                    source.DistanceSquared < 0 || source.DistanceSquared > settings.AwarenessDistance * settings.AwarenessDistance)
                {
                    continue;
                }

                bool heard = now <= source.HeardUntil;
                if (source.Seen || heard)
                {
                    remembered[source.Id] = now + settings.MemorySeconds;
                }

                if (!source.Seen && !heard && !remembered.ContainsKey(source.Id))
                {
                    continue;
                }

                PerceivedEnemies++;
                if (source.Engaged)
                {
                    EngagedEnemies++;
                    if (source.DistanceSquared <= settings.CloseDistance * settings.CloseDistance)
                    {
                        close++;
                    }
                }

                attack |= source.Attacking;
                if (now <= source.UrgentUntil)
                {
                    duckUntil = Math.Max(duckUntil, now + settings.CueDuck);
                }
            }

            if (PerceivedEnemies > 0)
            {
                lastThreat = now;
            }

            if (attack || close > 0 || EngagedEnemies >= settings.CrowdCount ||
                (EngagedEnemies > 0 && now - lastShot < settings.CombatAfterShot))
            {
                lastCombat = now;
            }

            Mood = SelectMood(now);
        }

        internal void ReportShot()
        {
            float now = Now;
            lastShot = now;
            if (EngagedEnemies > 0)
            {
                lastCombat = now;
                lastThreat = now;
            }

            duckUntil = now + settings.ShotDuck;
        }

        internal void ReportDamage()
        {
            float now = Now;
            lastCombat = now;
            lastThreat = now;
            duckUntil = now + settings.DamageDuck;
        }

        internal void Reset()
        {
            lastThreat = float.NegativeInfinity;
            lastCombat = float.NegativeInfinity;
            lastShot = float.NegativeInfinity;
            duckUntil = float.NegativeInfinity;
            nextObservation = 0;
            PerceivedEnemies = 0;
            EngagedEnemies = 0;
            Mood = MoodState.Explore;
            remembered.Clear();
            aliveSources.Clear();
            expired.Clear();
        }

        private void Prune(IReadOnlyList<AudioThreatObservation> sources, float now)
        {
            aliveSources.Clear();
            for (int index = 0; index < sources.Count; index++)
            {
                if (sources[index].Alive)
                {
                    aliveSources.Add(sources[index].Id);
                }
            }

            expired.Clear();
            foreach (KeyValuePair<int, float> pair in remembered)
            {
                if (!aliveSources.Contains(pair.Key) || pair.Value < now)
                {
                    expired.Add(pair.Key);
                }
            }

            for (int index = 0; index < expired.Count; index++)
            {
                remembered.Remove(expired[index]);
            }
        }

        private MoodState SelectMood(float now)
        {
            if (now - lastCombat < settings.CombatHold)
            {
                return MoodState.Combat;
            }

            return now - lastThreat < settings.ThreatHold ? MoodState.Threat : MoodState.Explore;
        }
    }
}
