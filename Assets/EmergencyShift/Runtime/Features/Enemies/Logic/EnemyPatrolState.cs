using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct EnemyPatrolState
    {
        public EnemyPatrolState(Vector2 origin, bool hasTarget, Vector2 target, float pause, float stalled, float previousDistance)
        {
            Origin = origin;
            HasTarget = hasTarget;
            Target = target;
            Pause = pause;
            Stalled = stalled;
            PreviousDistance = previousDistance;
        }

        public Vector2 Origin { get; }
        public bool HasTarget { get; }
        public Vector2 Target { get; }
        public float Pause { get; }
        public float Stalled { get; }
        public float PreviousDistance { get; }
    }
}
