using System;
using EmergencyShift.Core.Time;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion.Logic
{
    public enum ScorpionState { Waiting, Charging, Gathering, Attacking, Dead }

    /// <summary>Personal combat timeline. No light/fear state, animation callback or shared mutable health.</summary>
    public sealed class ScorpionModel
    {
        private readonly IGameClock clock;
        private readonly float attackSeconds;
        private double attackAt;
        private double diedAt;
        private float previousPhase;
        private double gatherAt;
        private float gatherPhase;
        private float attackStartProgress;
        private float motionSpeed;

        public ScorpionModel(IGameClock clock, float attackSeconds, Vector2 initialFacing = default)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (!float.IsFinite(attackSeconds) || attackSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSeconds));
            }
            this.attackSeconds = attackSeconds;
            Facing = initialFacing.sqrMagnitude > .001f ? initialFacing.normalized : Vector2.up;
        }

        public ScorpionState State { get; private set; }
        public bool HasAggro { get; private set; }
        public bool IsAlive => State != ScorpionState.Dead;
        public float GaitPhase { get; private set; }
        public Vector2 Heading { get; private set; }
        public Vector2 Facing { get; private set; }
        public float AttackProgress => State == ScorpionState.Attacking ? Mathf.Clamp01(attackStartProgress + (float)(clock.Time - attackAt) / attackSeconds) : 0;
        public float DeathTime => IsAlive ? 0 : Mathf.Max(0, (float)(clock.Time - diedAt));
        public bool CrossedGaitBoundary => GaitPhase < previousPhase || GaitPhase < .08f;

        public void StopMotion()
        {
            motionSpeed = 0;
        }

        public void CommitMotion(float speed)
        {
            motionSpeed = speed;
        }

        public Vector2 Steer(Vector2 preferred, float maximumDegrees, float accelerationStep, float brakingStep)
        {
            if (preferred.sqrMagnitude < .0001f)
            {
                motionSpeed = 0;
                return Vector2.zero;
            }
            AdvanceFacing(preferred, maximumDegrees);
            float alignment = Mathf.Max(0, Vector2.Dot(Facing, preferred.normalized));
            float targetSpeed = preferred.magnitude * alignment * alignment;
            motionSpeed = Mathf.MoveTowards(motionSpeed, targetSpeed, targetSpeed < motionSpeed ? brakingStep : accelerationStep);
            return Facing * motionSpeed;
        }

        public void AdvanceFacing(Vector2 preferred, float maximumDegrees)
        {
            if (State == ScorpionState.Attacking || !IsAlive || preferred.sqrMagnitude < .001f)
            {
                return;
            }
            float current = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg;
            float desired = Mathf.Atan2(preferred.y, preferred.x) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(current, desired, maximumDegrees) * Mathf.Deg2Rad;
            Facing = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public void AdvanceTravel(float distance, float distancePerCycle)
        {
            previousPhase = GaitPhase;
            if (State == ScorpionState.Charging && float.IsFinite(distance) && distance >= 0 && distance < distancePerCycle)
            {
                GaitPhase = Mathf.Repeat(GaitPhase + distance / distancePerCycle, 1);
            }
        }

        internal void Alert()
        {
            HasAggro = true;
            if (State == ScorpionState.Waiting)
            {
                State = ScorpionState.Charging;
            }
        }

        internal void BeginAttack(Vector2 heading)
        {
            Heading = Facing;
            // The attack contains two gait cycles. Carry a boundary overshoot into
            // its matching foot phase instead of snapping the feet backwards.
            attackStartProgress = GaitPhase * .5f;
            attackAt = clock.Time;
            State = ScorpionState.Attacking;
        }

        internal void Gather()
        {
            gatherPhase = GaitPhase;
            gatherAt = clock.Time;
            State = ScorpionState.Gathering;
        }

        public bool AdvanceGather(float cycleSeconds)
        {
            float phase = gatherPhase + (float)(clock.Time - gatherAt) / cycleSeconds;
            GaitPhase = Mathf.Clamp01(phase);
            if (phase < 1)
            {
                return false;
            }
            GaitPhase = 0;
            previousPhase = 0;
            State = ScorpionState.Charging;
            return true;
        }

        public bool FinishIfElapsed()
        {
            if (State != ScorpionState.Attacking || AttackProgress < 1)
            {
                return false;
            }
            State = ScorpionState.Charging;
            GaitPhase = 0;
            previousPhase = 0;
            return true;
        }

        internal void Kill()
        {
            State = ScorpionState.Dead;
            diedAt = clock.Time;
        }

        internal void CancelAttack()
        {
            State = ScorpionState.Charging;
            GaitPhase = 0;
            previousPhase = 0;
        }
    }
}
