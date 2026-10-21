using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class SleeperModel
    {
        private readonly SleeperSettings settings;
        private readonly IGameClock clock;
        private float age;
        private float exposure;
        private double staggerUntil;
        private Vector2 hitPush;

        public SleeperModel(SleeperSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public event Action<SleeperState> StateChanged;
        public SleeperState State { get; private set; } = SleeperState.Sleeping;
        public bool HasAggro { get; private set; }
        public bool IsAlive => State != SleeperState.Dead;
        public bool IsStaggered => IsAlive && clock.Time < staggerUntil;
        public bool CanStopTicking => !IsAlive && age >= 3;
        public float ExposureProgress => Mathf.Clamp01(exposure / settings.ExposureSeconds);
        public float Closure
        {
            get
            {
                if (State == SleeperState.Sleeping)
                {
                    return 1;
                }

                return State == SleeperState.Waking ? 1 - Mathf.Clamp01(age / settings.WakingSeconds) : 0;
            }
        }

        public float AttackProgress => State == SleeperState.Attacking ? Mathf.Clamp01(age / settings.AttackDuration) : 0;
        public float DeathTime => !IsAlive ? age : 0;
        public float PursuitSpeed => settings.PursuitSpeed;
        public float InterruptDelay => settings.AttackCooldown;
        public Vector2 AttackHeading { get; private set; }
        public Vector2 StaggerVelocity => hitPush * Mathf.Clamp01((float)(staggerUntil - clock.Time) / Mathf.Max(.01f, settings.StaggerSeconds));
        internal bool ValidClock => double.IsFinite(clock.Time) && clock.Time >= 0;

        public SleeperIntent Step(float deltaTime, bool targetAlive, bool exposed)
        {
            if (!ValidClock || !float.IsFinite(deltaTime) || deltaTime <= 0)
            {
                return SleeperIntent.Stop;
            }

            age += deltaTime;
            if (!IsAlive)
            {
                return SleeperIntent.Dead;
            }

            if (!targetAlive)
            {
                if (HasAggro)
                {
                    Enter(SleeperState.TargetDead);
                }

                return SleeperIntent.Stop;
            }

            if (State == SleeperState.Sleeping)
            {
                exposure = exposed ? exposure + deltaTime : Mathf.Max(0, exposure - settings.ExposureDecay * deltaTime);
                if (exposure >= settings.ExposureSeconds)
                {
                    Wake();
                }

                return SleeperIntent.Stop;
            }

            if (IsStaggered)
            {
                return SleeperIntent.Stagger;
            }

            switch (State)
            {
                case SleeperState.Waking:
                    if (age >= settings.WakingSeconds)
                    {
                        Enter(SleeperState.Hunting);
                    }

                    break;
                case SleeperState.Hunting:
                    return SleeperIntent.Hunt;
                case SleeperState.Attacking:
                    return SleeperIntent.Attack;
                case SleeperState.Recovering:
                    if (age >= settings.RecoverySeconds)
                    {
                        Enter(SleeperState.Hunting);
                    }

                    break;
            }

            return SleeperIntent.Stop;
        }

        public SleeperStateSnapshot Capture() => new SleeperStateSnapshot(State, HasAggro, age, exposure,
            (float)Math.Max(0, staggerUntil - clock.Time), hitPush, AttackHeading);

        public static CommandResult ValidateSnapshot(SleeperStateSnapshot state) =>
            Enum.IsDefined(typeof(SleeperState), state.State) && Nonnegative(state.Age) && Nonnegative(state.Exposure) &&
            Nonnegative(state.StaggerLeft) && Finite(state.Push) && Finite(state.Heading)
                ? CommandResult.Ok() : CommandResult.Fail("Invalid sleeper state, time or direction.");

        internal void Wake()
        {
            HasAggro = true;
            exposure = 0;
            Enter(SleeperState.Waking);
        }

        internal void BeginAttack(Vector2 heading)
        {
            AttackHeading = heading;
            Enter(SleeperState.Attacking);
        }

        internal void CompleteAttack() => Enter(SleeperState.Recovering);

        internal void Stagger(Vector2 direction)
        {
            staggerUntil = clock.Time + settings.StaggerSeconds;
            hitPush = direction.normalized * settings.PushSpeed;
            if (State == SleeperState.Attacking)
            {
                Enter(SleeperState.Recovering);
            }
        }

        internal void Die() => Enter(SleeperState.Dead);

        internal void Restore(SleeperStateSnapshot state)
        {
            State = state.State;
            HasAggro = state.Aggro;
            age = state.Age;
            exposure = state.Exposure;
            staggerUntil = clock.Time + state.StaggerLeft;
            hitPush = state.Push;
            AttackHeading = state.Heading;
        }

        private void Enter(SleeperState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            age = 0;
            StateChanged?.Invoke(state);
        }

        internal static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.sqrMagnitude);
        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
    }
}
