namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct EnemyMeleeObservation
    {
        public EnemyMeleeObservation(bool hasDamageableTarget, bool targetAlive, float squaredDistance, float headingAngle)
        {
            HasDamageableTarget = hasDamageableTarget;
            TargetAlive = targetAlive;
            SquaredDistance = squaredDistance;
            HeadingAngle = headingAngle;
        }

        public bool HasDamageableTarget { get; }
        public bool TargetAlive { get; }
        public float SquaredDistance { get; }
        public float HeadingAngle { get; }
        public bool IsValid => float.IsFinite(SquaredDistance) && SquaredDistance >= 0 &&
            float.IsFinite(HeadingAngle) && HeadingAngle >= 0 && HeadingAngle <= 180;
    }
}
