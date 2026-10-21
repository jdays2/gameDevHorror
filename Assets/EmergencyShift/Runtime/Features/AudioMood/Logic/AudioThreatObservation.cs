namespace EmergencyShift.Features.AudioMood.Logic
{
    /// <summary>Transient membership identity and sensed values, never a persistent enemy ID.</summary>
    public readonly struct AudioThreatObservation
    {
        public int Id { get; }
        public bool Alive { get; }
        public bool Eligible { get; }
        public float DistanceSquared { get; }
        public bool Seen { get; }
        public float HeardUntil { get; }
        public float UrgentUntil { get; }
        public bool Engaged { get; }
        public bool Attacking { get; }

        public AudioThreatObservation(int id, bool alive, bool eligible, float distanceSquared,
            bool seen, float heardUntil, float urgentUntil, bool engaged, bool attacking)
        {
            Id = id;
            Alive = alive;
            Eligible = eligible;
            DistanceSquared = distanceSquared;
            Seen = seen;
            HeardUntil = heardUntil;
            UrgentUntil = urgentUntil;
            Engaged = engaged;
            Attacking = attacking;
        }
    }
}
