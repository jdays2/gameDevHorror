namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct EnemyMeleeState
    {
        public EnemyMeleeState(bool struck, bool lunged, float cooldown)
        {
            Struck = struck;
            Lunged = lunged;
            Cooldown = cooldown;
        }

        public bool Struck { get; }
        public bool Lunged { get; }
        public float Cooldown { get; }
    }
}
