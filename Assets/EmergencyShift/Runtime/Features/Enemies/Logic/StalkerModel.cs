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
        private float age;
        private float exposure;
        private float burnContact;
        private double nextBurnCue;
        private bool burnActive;
        private Vector2 approach;
        private Vector2 escape;
        private double staggerUntil;
        private Vector2 push;

        public StalkerModel(StalkerSettings settings, IGameClock clock, Vector2 home)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (!Finite(home))
            {
                throw new ArgumentException("Stalker needs a finite spawn origin.", nameof(home));
            }

            Home = home;
        }

        public event Action<StalkerState> StateChanged;
        public event Action EyeBurnStarted;
        public event Action PatrolResetRequested;
        public StalkerState State { get; private set; } = StalkerState.Dormant;
        public bool HasAggro { get; private set; }
        public bool IsAlive => State != StalkerState.Dead;
        public bool IsStaggered => IsAlive && clock.Time < staggerUntil;
        public bool CanStopTicking => !IsAlive && age >= 3;
        public bool Exposed { get; private set; }
        public float EyeDamageProgress { get; private set; }
        public float Closure { get; private set; }
        public Vector2 Home { get; }
        public float PursuitSpeed => settings.PursuitSpeed;
        public float RetreatSpeed => settings.RetreatSpeed;
        public float FearDistance => settings.FearDistance;
        public float ReactionDelay => settings.ReactionDelay;
        public float ReactionProgress => Mathf.Clamp01(exposure / settings.ReactionDelay);
        public float AttackProgress => State == StalkerState.Attacking ? Mathf.Clamp01(age / settings.AttackDuration) : 0;
        public float DeathTime => !IsAlive ? age : 0;
        public Vector2 Escape => escape;
        public Vector2 Approach => approach;
        public float InterruptDelay => settings.InterruptDelay;
        public Vector2 StaggerVelocity => push * Mathf.Clamp01((float)(staggerUntil - clock.Time) / Mathf.Max(.01f, settings.StaggerSeconds));
        internal bool ValidClock => double.IsFinite(clock.Time) && clock.Time >= 0;
        internal bool CanBeginAttack => IsAlive && State == StalkerState.Sneaking && HasAggro && Closure < .1f;

        public StalkerIntent Step(StalkerObservation frame)
        {
            if (!ValidClock || !frame.IsValid)
            {
                return StalkerIntent.DirectStop;
            }

            age += frame.DeltaTime;
            if (!IsAlive || !frame.TargetReady)
            {
                return StalkerIntent.DirectStop;
            }

            if (!frame.TargetAlive)
            {
                PatrolResetRequested?.Invoke();
                Exposed = false;
                exposure = 0;
                UpdateEyeBurn(false, frame.DeltaTime);
                Enter(StalkerState.TargetDead);
                return StalkerIntent.DirectStop;
            }

            if (!HasAggro && Vector2.Distance(frame.Position, frame.TargetPosition) <= frame.DetectionRadius && frame.LineOfSight)
            {
                ActivateAggro();
            }

            Exposed = frame.Exposed;
            bool closeLight = Exposed && Vector2.Distance(frame.Position, frame.LightOrigin) <= settings.FearDistance;
            UpdateEyeBurn(closeLight, frame.DeltaTime);
            if (IsStaggered)
            {
                return StalkerIntent.Stagger;
            }

            bool idle = State == StalkerState.Dormant || State == StalkerState.Patrolling;
            if ((idle || State == StalkerState.Sneaking || State == StalkerState.Attacking) && closeLight)
            {
                if (exposure == 0)
                {
                    approach = frame.Velocity.sqrMagnitude > .01f ? frame.Velocity.normalized : (frame.TargetPosition - frame.Position).normalized;
                }

                exposure += frame.DeltaTime;
                if (exposure >= settings.ReactionDelay)
                {
                    escape = (frame.Position - frame.LightOrigin).normalized;
                    if (escape.sqrMagnitude < .001f)
                    {
                        escape = -frame.LightDirection;
                    }

                    Enter(StalkerState.Recoiling);
                }
            }
            else
            {
                exposure = 0;
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
                    Closure = Mathf.Clamp01(age / settings.RecoilSeconds);
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
                    if (age >= settings.CooldownSeconds && !Exposed)
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

        public bool ShouldFollow(StalkerObservation frame, Vector2 goal) =>
            Vector2.Distance(goal, frame.Position) > .15f &&
            Vector2.Distance(frame.TargetPosition, frame.Position) > settings.StandOffDistance * .8f;

        public StalkerStateSnapshot Capture() => new StalkerStateSnapshot(State, HasAggro, EyeDamageProgress, burnContact,
            age, exposure, Closure, escape, approach, (float)Math.Max(0, staggerUntil - clock.Time), push);

        public static CommandResult ValidateSnapshot(StalkerStateSnapshot state) =>
            Enum.IsDefined(typeof(StalkerState), state.State) && Unit(state.EyeDamage) && Unit(state.Closure) &&
            Nonnegative(state.BurnContact) && Nonnegative(state.Age) && Nonnegative(state.Exposure) && Nonnegative(state.StaggerLeft) &&
            Finite(state.Escape) && Finite(state.Approach) && Finite(state.Push)
                ? CommandResult.Ok() : CommandResult.Fail("Invalid Stalker state, timers, closure, eye damage or directions.");

        internal void ActivateAggro()
        {
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
            Closure = 1;
            Enter(StalkerState.Dead);
            PatrolResetRequested?.Invoke();
        }

        internal void Restore(StalkerStateSnapshot state)
        {
            State = state.State;
            HasAggro = state.Aggro;
            EyeDamageProgress = state.EyeDamage;
            burnContact = state.BurnContact;
            burnActive = burnContact > 0;
            nextBurnCue = clock.Time + settings.EyeBurnCue;
            age = state.Age;
            exposure = state.Exposure;
            Closure = state.Closure;
            escape = state.Escape;
            approach = state.Approach;
            staggerUntil = clock.Time + state.StaggerLeft;
            push = state.Push;
        }

        private void UpdateEyeBurn(bool illuminated, float deltaTime)
        {
            if (!illuminated)
            {
                burnContact = 0;
                burnActive = false;
                return;
            }

            if (EyeDamageProgress >= 1)
            {
                return;
            }

            if (!burnActive)
            {
                burnActive = true;
                if (clock.Time >= nextBurnCue)
                {
                    nextBurnCue = clock.Time + settings.EyeBurnCue;
                    EyeBurnStarted?.Invoke();
                }
            }

            float previousDose = Mathf.Max(0, burnContact - settings.EyeBurnLead);
            burnContact += deltaTime;
            float dose = Mathf.Max(0, burnContact - settings.EyeBurnLead) - previousDose;
            EyeDamageProgress = Mathf.Clamp01(EyeDamageProgress + dose / settings.EyeBurnSeconds);
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
