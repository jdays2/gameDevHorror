using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.ClawEnemy.Config;
using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.Logic
{
    public enum ClawEnemyState { Waiting, Patrolling, Chasing, Attacking, Staggered, Dead }
    public enum ClawEnemyIntent { Stop, Patrol, Pursue, Attack, FinishAttack, Push }

    public sealed class ClawEnemyModel
    {
        private readonly ClawEnemySettings settings;
        private readonly IGameClock clock;
        private double attackAt;
        private double staggerUntil;

        public ClawEnemyModel(ClawEnemySettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public ClawEnemyState State { get; private set; }
        public bool HasAggro { get; private set; }
        public bool IsAlive => State != ClawEnemyState.Dead;
        public float Speed => settings.Speed;
        public float StaggerSeconds => settings.StaggerSeconds;
        public Vector2 Heading { get; private set; }
        public Vector2 Push { get; private set; }
        public float AttackProgress => State == ClawEnemyState.Attacking ? Mathf.Clamp01((float)(clock.Time - attackAt) / settings.AttackSeconds) : 0;

        public ClawEnemyIntent Step(ClawEnemyObservation frame)
        {
            if (!IsAlive)
            {
                return ClawEnemyIntent.Stop;
            }
            if (!frame.TargetAlive || frame.Stationary)
            {
                State = ClawEnemyState.Waiting;
                return ClawEnemyIntent.Stop;
            }
            if (frame.Detected)
            {
                HasAggro = true;
            }
            if (State == ClawEnemyState.Attacking)
            {
                if (AttackProgress < 1)
                {
                    return ClawEnemyIntent.Attack;
                }
                State = ClawEnemyState.Chasing;
                return ClawEnemyIntent.FinishAttack;
            }
            if (clock.Time < staggerUntil)
            {
                State = ClawEnemyState.Staggered;
                return ClawEnemyIntent.Push;
            }
            if (HasAggro)
            {
                State = ClawEnemyState.Chasing;
                return ClawEnemyIntent.Pursue;
            }
            State = frame.Patrol ? ClawEnemyState.Patrolling : ClawEnemyState.Waiting;
            return frame.Patrol ? ClawEnemyIntent.Patrol : ClawEnemyIntent.Stop;
        }

        internal void BeginAttack(Vector2 heading)
        {
            Heading = heading.normalized;
            attackAt = clock.Time;
            State = ClawEnemyState.Attacking;
        }

        internal void Hit(Vector2 heading)
        {
            HasAggro = true;
            staggerUntil = clock.Time + settings.StaggerSeconds;
            Push = heading.normalized * settings.PushSpeed;
            State = ClawEnemyState.Staggered;
        }

        internal void Kill() => State = ClawEnemyState.Dead;
    }
}
