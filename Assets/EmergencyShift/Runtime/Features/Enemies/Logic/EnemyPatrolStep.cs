using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct EnemyPatrolStep
    {
        public EnemyPatrolStep(bool enabled, Vector2 position, float radius, EnemyPatrolBounds bounds, int solids, float deltaTime)
        {
            Enabled = enabled;
            Position = position;
            Radius = radius;
            Bounds = bounds;
            Solids = solids;
            DeltaTime = deltaTime;
        }

        public bool Enabled { get; }
        public Vector2 Position { get; }
        public float Radius { get; }
        public EnemyPatrolBounds Bounds { get; }
        public int Solids { get; }
        public float DeltaTime { get; }
        public bool IsValid => EnemyPatrolBounds.Finite(Position) && float.IsFinite(Position.sqrMagnitude) &&
            float.IsFinite(Radius) && Radius >= 0 && Bounds.IsValid && float.IsFinite(DeltaTime) && DeltaTime > 0;
    }
}
