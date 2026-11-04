using System;

namespace EmergencyShift.Features.Scorpion.Logic
{
    /// <summary>Immutable motion/timeline snapshot; distances are world units and rates are per second.</summary>
    public sealed class ScorpionSettings
    {
        public ScorpionSettings(float pursuitSpeed, float attackSpeed, float attackSeconds, float distancePerCycle,
            float turnDegrees, float acceleration, float braking)
        {
            if (!float.IsFinite(pursuitSpeed) || pursuitSpeed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pursuitSpeed));
            }
            if (!float.IsFinite(attackSpeed) || attackSpeed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSpeed));
            }
            if (!float.IsFinite(attackSeconds) || attackSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSeconds));
            }
            if (!float.IsFinite(distancePerCycle) || distancePerCycle <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distancePerCycle));
            }
            if (!float.IsFinite(turnDegrees) || turnDegrees <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turnDegrees));
            }
            if (!float.IsFinite(acceleration) || acceleration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(acceleration));
            }
            if (!float.IsFinite(braking) || braking <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(braking));
            }
            float gatherCycleSeconds = distancePerCycle / attackSpeed;
            if (!float.IsFinite(gatherCycleSeconds) || gatherCycleSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distancePerCycle), "Gather cycle must have a finite positive duration.");
            }
            PursuitSpeed = pursuitSpeed;
            AttackSpeed = attackSpeed;
            AttackSeconds = attackSeconds;
            DistancePerCycle = distancePerCycle;
            TurnDegrees = turnDegrees;
            Acceleration = acceleration;
            Braking = braking;
            GatherCycleSeconds = gatherCycleSeconds;
        }

        public float PursuitSpeed { get; }
        public float AttackSpeed { get; }
        public float AttackSeconds { get; }
        public float DistancePerCycle { get; }
        public float TurnDegrees { get; }
        public float Acceleration { get; }
        public float Braking { get; }
        public float GatherCycleSeconds { get; }
    }
}
