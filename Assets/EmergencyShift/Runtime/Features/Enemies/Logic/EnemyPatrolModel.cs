using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>Personal route and pause state; three explicit dependencies are config, RNG and geometry.</summary>
    public sealed class EnemyPatrolModel
    {
        private const float DeltaEpsilon = .0001f;
        private readonly EnemyPatrolSettings settings;
        private readonly IRandomSource random;
        private readonly IEnemyPatrolEnvironment environment;
        private Vector2 origin;
        private bool hasTarget;
        private Vector2 target;
        private float pauseLeft;
        private float stalledFor;
        private float previousDistance;

        public EnemyPatrolModel(EnemyPatrolSettings settings, IRandomSource random, IEnemyPatrolEnvironment environment)
        {
            // An inactive authored module can omit its profile; enabling motion requires one.
            this.settings = settings;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public bool IsInitialized { get; private set; }
        public Vector2 Origin => origin;
        public bool HasTarget => hasTarget;
        public Vector2 Target => target;
        public bool IsPausing => pauseLeft > 0;

        public Vector2 Step(EnemyPatrolStep frame)
        {
            if (!frame.Enabled || !frame.IsValid || !IsInitialized)
            {
                return Vector2.zero;
            }

            if (settings == null)
            {
                throw new InvalidOperationException("Enabled patrol requires its authored profile.");
            }

            Rect area = frame.Bounds.Area(origin);
            Vector2 padding = Vector2.one * (frame.Radius + settings.Skin);
            Vector2 min = area.min + padding;
            Vector2 max = area.max - padding;
            if (!EnemyPatrolBounds.Finite(min) || !EnemyPatrolBounds.Finite(max) ||
                (frame.Bounds.UseArea && (min.x >= max.x || min.y >= max.y)))
            {
                return Vector2.zero;
            }

            if (pauseLeft > 0)
            {
                pauseLeft = Mathf.Max(0, pauseLeft - frame.DeltaTime);
                return Vector2.zero;
            }

            if (hasTarget && ((frame.Bounds.UseArea && !EnemyPatrolBounds.Contains(target, min, max)) ||
                Vector2.Distance(frame.Position, target) < settings.ArrivalDistance))
            {
                Pause();
                return Vector2.zero;
            }

            if (!hasTarget)
            {
                for (int attempt = 0; attempt < settings.Attempts; attempt++)
                {
                    Vector2 candidate = frame.Bounds.UseArea
                        ? new Vector2(Between(min.x, max.x), Between(min.y, max.y))
                        : frame.Position + InsideCircle() * settings.WanderDistance;
                    Vector2 delta = candidate - frame.Position;
                    float distance = delta.magnitude;
                    if (!float.IsFinite(distance) || distance < settings.MinimumCandidateDistance ||
                        environment.IsBlocked(frame.Position, frame.Radius, delta.normalized, distance, frame.Solids))
                    {
                        continue;
                    }

                    target = candidate;
                    hasTarget = true;
                    previousDistance = distance;
                    stalledFor = 0;
                    break;
                }

                if (!hasTarget)
                {
                    pauseLeft = settings.RetryPause;
                    return Vector2.zero;
                }
            }

            Vector2 toTarget = target - frame.Position;
            float remaining = toTarget.magnitude;
            stalledFor = previousDistance - remaining > frame.DeltaTime * settings.Speed * settings.ProgressFraction
                ? 0 : stalledFor + frame.DeltaTime;
            previousDistance = remaining;
            float step = Mathf.Min(settings.Speed * frame.DeltaTime, remaining);
            if (stalledFor > settings.StalledSeconds || environment.IsBlocked(frame.Position, frame.Radius,
                toTarget.normalized, step + settings.Skin, frame.Solids))
            {
                Pause();
                return Vector2.zero;
            }

            return toTarget.normalized * (step / Mathf.Max(DeltaEpsilon, frame.DeltaTime));
        }

        public EnemyPatrolState Capture() => new EnemyPatrolState(origin, hasTarget, target, pauseLeft, stalledFor, previousDistance);

        public static CommandResult ValidateSnapshot(EnemyPatrolState state) =>
            EnemyPatrolBounds.Finite(state.Origin) && EnemyPatrolBounds.Finite(state.Target) &&
            Nonnegative(state.Pause) && Nonnegative(state.Stalled) && Nonnegative(state.PreviousDistance)
                ? CommandResult.Ok() : CommandResult.Fail("Patrol snapshot requires finite positions and nonnegative timers/distance.");

        internal void Initialize(Vector2 spawn)
        {
            origin = spawn;
            IsInitialized = true;
            Reset();
        }

        internal void Reset()
        {
            hasTarget = false;
            pauseLeft = 0;
            stalledFor = 0;
        }

        internal void Restore(EnemyPatrolState state)
        {
            origin = state.Origin;
            IsInitialized = true;
            hasTarget = state.HasTarget;
            target = state.Target;
            pauseLeft = state.Pause;
            stalledFor = state.Stalled;
            previousDistance = state.PreviousDistance;
        }

        private void Pause()
        {
            hasTarget = false;
            stalledFor = 0;
            pauseLeft = Between(settings.PauseSeconds.x, settings.PauseSeconds.y);
        }

        private float Between(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, Unit());

        private Vector2 InsideCircle()
        {
            float radius = Mathf.Sqrt(Unit());
            float angle = Unit() * Mathf.PI * 2;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private float Unit()
        {
            double value = random.NextUnit();
            if (!double.IsFinite(value) || value < 0 || value > 1)
            {
                throw new InvalidOperationException("Patrol RNG must return a finite unit value.");
            }

            return (float)value;
        }

        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
    }
}
