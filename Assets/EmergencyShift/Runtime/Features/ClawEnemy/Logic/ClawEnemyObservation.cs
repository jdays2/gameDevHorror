using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.Logic
{
    public readonly struct ClawEnemyObservation
    {
        public ClawEnemyObservation(float deltaTime, Vector2 position, Vector2 target, bool targetAlive,
            bool detected, bool patrol, bool attacks, bool stationary, float radius, int solids)
        {
            DeltaTime = deltaTime;
            Position = position;
            Target = target;
            TargetAlive = targetAlive;
            Detected = detected;
            Patrol = patrol;
            Attacks = attacks;
            Stationary = stationary;
            Radius = radius;
            Solids = solids;
        }

        public float DeltaTime { get; }
        public Vector2 Position { get; }
        public Vector2 Target { get; }
        public bool TargetAlive { get; }
        public bool Detected { get; }
        public bool Patrol { get; }
        public bool Attacks { get; }
        public bool Stationary { get; }
        public float Radius { get; }
        public int Solids { get; }
    }
}
