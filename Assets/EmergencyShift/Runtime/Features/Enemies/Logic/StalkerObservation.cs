using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct StalkerObservation
    {
        public StalkerObservation(float deltaTime, Vector2 position, Vector2 velocity, Vector2 targetPosition,
            Vector2 lightOrigin, Vector2 lightDirection, bool targetReady, bool targetAlive, bool lineOfSight,
            bool exposed, bool patrolEnabled, bool attacksEnabled, float detectionRadius, float bodyRadius, int solids)
        {
            DeltaTime = deltaTime;
            Position = position;
            Velocity = velocity;
            TargetPosition = targetPosition;
            LightOrigin = lightOrigin;
            LightDirection = lightDirection;
            TargetReady = targetReady;
            TargetAlive = targetAlive;
            LineOfSight = lineOfSight;
            Exposed = exposed;
            PatrolEnabled = patrolEnabled;
            AttacksEnabled = attacksEnabled;
            DetectionRadius = detectionRadius;
            BodyRadius = bodyRadius;
            Solids = solids;
        }

        public float DeltaTime { get; }
        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public Vector2 TargetPosition { get; }
        public Vector2 LightOrigin { get; }
        public Vector2 LightDirection { get; }
        public bool TargetReady { get; }
        public bool TargetAlive { get; }
        public bool LineOfSight { get; }
        public bool Exposed { get; }
        public bool PatrolEnabled { get; }
        public bool AttacksEnabled { get; }
        public float DetectionRadius { get; }
        public float BodyRadius { get; }
        public int Solids { get; }
        public bool IsValid => float.IsFinite(DeltaTime) && DeltaTime > 0 && StalkerModel.Finite(Position) &&
            StalkerModel.Finite(Velocity) && StalkerModel.Finite(TargetPosition) && StalkerModel.Finite(LightOrigin) &&
            StalkerModel.Finite(LightDirection) && float.IsFinite(DetectionRadius) && DetectionRadius >= 0 &&
            float.IsFinite(BodyRadius) && BodyRadius >= 0;
    }
}
