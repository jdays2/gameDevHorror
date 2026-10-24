using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class StalkerModel
    {
        private readonly StalkerSettings settings;
        private readonly IGameClock clock;
        private readonly StalkerEyeModel eye;
        private float age;
        private Vector2 approach;
        private Vector2 escape;
        private double staggerUntil;
        private Vector2 push;

        public StalkerModel(StalkerSettings settings, IGameClock clock, Vector2 home)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            eye = new StalkerEyeModel(settings);
            if (!Finite(home))
            {
                throw new ArgumentException("Stalker needs a finite spawn origin.", nameof(home));
            }

            Home = home;
        }

        public event Action<StalkerState> StateChanged;
        public event Action EyeBurnStarted
        {
            add => eye.BurnStarted += value;
            remove => eye.BurnStarted -= value;
        }
        public event Action PatrolResetRequested;
        public StalkerState State { get; private set; } = StalkerState.Dormant;
        public bool HasAggro { get; private set; }
        public bool IsScripted { get; private set; }
        public bool IsAlive => State != StalkerState.Dead;
        public bool IsStaggered => IsAlive && clock.Time < staggerUntil;
        public bool CanStopTicking => !IsAlive && age >= 3;
        public bool Exposed { get; private set; }
        public float EyeDamageProgress => eye.Damage;
        public float EyeWarning => eye.Warning;
        public float Closure { get; private set; }
        public Vector2 Home { get; }
        public float PursuitSpeed => settings.PursuitSpeed;
        public float RetreatSpeed => settings.RetreatSpeed;
        public float FearDistance => settings.FearDistance;
        public float ReactionDelay => settings.ReactionDelay;
        public float RecoilSeconds => settings.RecoilSeconds;
        public float ReactionProgress => Mathf.Clamp01(eye.Exposure / settings.ReactionDelay);
        public float AttackProgress => State == StalkerState.Attacking ? Mathf.Clamp01(age / settings.AttackDuration) : 0;
        public float DeathTime => !IsAlive ? age : 0;
        public Vector2 Escape => escape;
        public Vector2 Approach => approach;
        public float InterruptDelay => settings.InterruptDelay;
        public Vector2 StaggerVelocity => push * Mathf.Clamp01((float)(staggerUntil - clock.Time) / Mathf.Max(.01f, settings.StaggerSeconds));
        internal bool ValidClock => double.IsFinite(clock.Time) && clock.Time >= 0;
        internal bool CanBeginAttack => IsAlive && !IsScripted && State == StalkerState.Sneaking && HasAggro && Closure < .1f;

        public StalkerIntent Step(StalkerObservation frame)
        {
            if (!ValidClock || !frame.IsValid)
            {
                return StalkerIntent.DirectStop;
            }

            age += frame.DeltaTime;
            Exposed = IsAlive && frame.TargetReady && frame.TargetAlive && frame.Exposed;
            float lightDistance = Vector2.Distance(frame.Position, frame.LightOrigin);
            bool closeLight = Exposed && lightDistance <= settings.FearDistance;
            bool idle = State == StalkerState.Dormant || State == StalkerState.Patrolling;
            bool canReact = IsScripted || idle || State == StalkerState.Sneaking || State == StalkerState.Attacking;
            if (IsAlive)
            {
                if (closeLight && eye.Exposure == 0)
                {
                    approach = frame.Velocity.sqrMagnitude > .01f ? frame.Velocity.normalized : (frame.TargetPosition - frame.Position).normalized;
                }

                eye.Step(Exposed, lightDistance, canReact && !IsStaggered, frame.DeltaTime);
            }

            if (IsScripted && IsAlive)
            {
                return IsStaggered ? StalkerIntent.Stagger : StalkerIntent.DirectStop;
            }

            if (!IsAlive || !frame.TargetReady)
            {
                return StalkerIntent.DirectStop;
            }

            if (!frame.TargetAlive)
            {
                PatrolResetRequested?.Invoke();
                eye.ResetReaction();
                Enter(StalkerState.TargetDead);
                return StalkerIntent.DirectStop;
            }

            if (!HasAggro && Vector2.Distance(frame.Position, frame.TargetPosition) <= frame.DetectionRadius && frame.LineOfSight)
            {
                ActivateAggro();
            }

            if (IsStaggered)
            {
                return StalkerIntent.Stagger;
            }

            if (canReact && eye.Ready)
            {
                escape = (frame.Position - frame.LightOrigin).normalized;
                if (escape.sqrMagnitude < .001f)
                {
                    escape = -frame.LightDirection;
                }

                Enter(StalkerState.Recoiling);
            }

            switch (State)
            {
                case StalkerState.Dormant:
                case StalkerState.Patrolling:
                    Closure = Mathf.MoveTowards(Closure, 0, frame.DeltaTime * 3);
                    if (frame.PatrolEnabled)
                    {
                        if (State != StalkerState.Patrolling)
                        {
                            Enter(StalkerState.Patrolling);
                        }

                        return StalkerIntent.Patrol;
                    }

                    if (State != StalkerState.Dormant)
                    {
                        PatrolResetRequested?.Invoke();
                        Enter(StalkerState.Dormant);
                    }

                    break;
                case StalkerState.Sneaking:
                    Closure = Mathf.MoveTowards(Closure, 0, frame.DeltaTime * 3);
                    if (closeLight)
                    {
                        return StalkerIntent.Approach;
                    }

                    return HasAggro ? StalkerIntent.Pursue : StalkerIntent.Stop;
                case StalkerState.Recoiling:
                    Closure = LightReactionClosure(age / settings.RecoilSeconds);
                    if (age >= settings.RecoilSeconds)
                    {
                        Enter(StalkerState.Retreating);
                    }

                    break;
                case StalkerState.Retreating:
                    Closure = 1;
                    if (age >= settings.RetreatSeconds)
                    {
                        Enter(StalkerState.Waiting);
                        break;
                    }

                    return StalkerIntent.Retreat;
                case StalkerState.Waiting:
                    Closure = 1;
                    if (age >= settings.CooldownSeconds && !eye.Critical)
                    {
                        Enter(HasAggro ? StalkerState.Sneaking : StalkerState.Dormant);
                    }

                    break;
                case StalkerState.Attacking:
                    Closure = 0;
                    return StalkerIntent.Attack;
            }

            return StalkerIntent.Stop;
        }

        public Vector2 PursuitCandidate(StalkerObservation frame) => frame.LineOfSight
            ? frame.TargetPosition - frame.LightDirection * settings.StandOffDistance : frame.TargetPosition;

        public float LightReactionClosure(float progress) => Mathf.InverseLerp(settings.EyeOpenRecoilFraction, 1, progress);

        public bool ShouldFollow(StalkerObservation frame, Vector2 goal) =>
            Vector2.Distance(goal, frame.Position) > .15f &&
            Vector2.Distance(frame.TargetPosition, frame.Position) > settings.StandOffDistance * .8f;

        public StalkerStateSnapshot Capture() => new StalkerStateSnapshot(State, HasAggro, EyeDamageProgress, eye.BurnContact,
            age, eye.Exposure, Closure, escape, approach, (float)Math.Max(0, staggerUntil - clock.Time), push);

        public static CommandResult ValidateSnapshot(StalkerStateSnapshot state) =>
            Enum.IsDefined(typeof(StalkerState), state.State) && Unit(state.EyeDamage) && Unit(state.Closure) &&
            Nonnegative(state.BurnContact) && Nonnegative(state.Age) && Nonnegative(state.Exposure) && Nonnegative(state.StaggerLeft) &&
            Finite(state.Escape) && Finite(state.Approach) && Finite(state.Push)
                ? CommandResult.Ok() : CommandResult.Fail("Invalid Stalker state, timers, closure, eye damage or directions.");

        internal void ActivateAggro()
        {
            if (IsScripted)
            {
                return;
            }

            HasAggro = true;
            PatrolResetRequested?.Invoke();
            if (State == StalkerState.Dormant || State == StalkerState.Patrolling)
            {
                Enter(StalkerState.Sneaking);
            }
        }

        internal void BeginAttack(Vector2 heading)
        {
            Enter(StalkerState.Attacking);
            escape = heading;
        }

        internal void CompleteAttack() => Enter(StalkerState.Sneaking);

        internal void Stagger(Vector2 direction, bool targetAlive)
        {
            if (targetAlive && !HasAggro)
            {
                ActivateAggro();
            }

            staggerUntil = clock.Time + settings.StaggerSeconds;
            push = direction.normalized * settings.PushSpeed;
            if (State == StalkerState.Attacking)
            {
                Enter(StalkerState.Sneaking);
            }
        }

        internal void Die()
        {
            IsScripted = false;
            Closure = 1;
            Enter(StalkerState.Dead);
            PatrolResetRequested?.Invoke();
        }

        internal void SetScriptedControl(bool active)
        {
            IsScripted = active;
            eye.ResetReaction();
            Closure = 0;
            PatrolResetRequested?.Invoke();
            Enter(StalkerState.Dormant);
        }

        internal void Restore(StalkerStateSnapshot state)
        {
            State = state.State;
            HasAggro = state.Aggro;
            eye.Restore(state.EyeDamage, state.BurnContact, state.Exposure,
                state.State == StalkerState.Recoiling || state.State == StalkerState.Retreating || state.State == StalkerState.Waiting);
            age = state.Age;
            Closure = state.Closure;
            escape = state.Escape;
            approach = state.Approach;
            staggerUntil = clock.Time + state.StaggerLeft;
            push = state.Push;
        }

        private void Enter(StalkerState state)
        {
            bool changed = State != state;
            State = state;
            age = 0;
            if (changed)
            {
                StateChanged?.Invoke(state);
            }
        }

        internal static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.sqrMagnitude);
        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
        private static bool Unit(float value) => Nonnegative(value) && value <= 1;
    }
}
