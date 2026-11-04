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
        private double attackAt;
        private double diedAt;
        private float previousPhase;
        private double gatherAt;
        private float gatherPhase;
        private float attackStartProgress;
        private float motionSpeed;

        public ScorpionModel(IGameClock clock, ScorpionSettings settings, Vector2 initialFacing = default)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Facing = initialFacing.sqrMagnitude > .001f ? initialFacing.normalized : Vector2.up;
        }

        public ScorpionSettings Settings { get; }
        public ScorpionState State { get; private set; }
        public bool HasAggro { get; private set; }
        public bool IsAlive => State != ScorpionState.Dead;
        public float GaitPhase { get; private set; }
        public Vector2 Heading { get; private set; }
        public Vector2 Facing { get; private set; }
        public float AttackProgress => State == ScorpionState.Attacking ? Mathf.Clamp01(attackStartProgress + (float)(clock.Time - attackAt) / Settings.AttackSeconds) : 0;
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

        public Vector2 Steer(Vector2 preferred, float deltaTime)
        {
            if (preferred.sqrMagnitude < .0001f)
            {
                motionSpeed = 0;
                return Vector2.zero;
            }
            AdvanceFacing(preferred, Settings.TurnDegrees * deltaTime);
            float alignment = Mathf.Max(0, Vector2.Dot(Facing, preferred.normalized));
            float targetSpeed = preferred.magnitude * alignment * alignment;
            float rate = targetSpeed < motionSpeed ? Settings.Braking : Settings.Acceleration;
            motionSpeed = Mathf.MoveTowards(motionSpeed, targetSpeed, rate * deltaTime);
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

        public void AdvanceTravel(float distance)
        {
            previousPhase = GaitPhase;
            if (State == ScorpionState.Charging && float.IsFinite(distance) && distance >= 0 && distance < Settings.DistancePerCycle)
            {
                GaitPhase = Mathf.Repeat(GaitPhase + distance / Settings.DistancePerCycle, 1);
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

        public bool AdvanceGather()
        {
            float phase = gatherPhase + (float)(clock.Time - gatherAt) / Settings.GatherCycleSeconds;
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
