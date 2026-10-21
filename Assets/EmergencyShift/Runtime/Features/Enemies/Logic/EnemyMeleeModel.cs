using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>Per-actor strike flags and cooldown; no physics, target object or presentation.</summary>
    public sealed class EnemyMeleeModel
    {
        private readonly EnemyMeleeSettings settings;
        private readonly IGameClock clock;
        private bool struck;
        private bool lunged;
        private double readyAt;

        public EnemyMeleeModel(EnemyMeleeSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public int Damage => settings.Damage;
        public float LungeSpeed => settings.LungeSpeed;
        public bool IsReady => ValidTime && clock.Time >= readyAt;
        public bool ValidTime => double.IsFinite(clock.Time) && clock.Time >= 0;

        public bool CanStart(EnemyMeleeObservation target) => IsReady && ValidTarget(target) && target.SquaredDistance <= settings.AttackDistance * settings.AttackDistance;

        public bool CanBite(EnemyMeleeObservation target, bool fixedHeading) => ValidTarget(target) &&
            Mathf.Sqrt(target.SquaredDistance) < settings.BiteDistance && (!fixedHeading || target.HeadingAngle <= settings.ArcDegrees * .5f);

        public bool IsMoving(float progress) => ValidProgress(progress) && progress >= settings.LungeStart && progress < settings.LungeEnd;

        public EnemyMeleeState Capture() => new EnemyMeleeState(struck, lunged, (float)Math.Max(0, readyAt - clock.Time));

        public static CommandResult ValidateSnapshot(EnemyMeleeState state) => float.IsFinite(state.Cooldown) && state.Cooldown >= 0
            ? CommandResult.Ok() : CommandResult.Fail("Melee cooldown must be finite and nonnegative.");

        internal bool AdvanceLunge(float progress)
        {
            if (lunged || !ValidProgress(progress) || progress < settings.LungeStart)
            {
                return false;
            }

            lunged = true;
            return true;
        }

        internal bool AdvanceStrike(float progress)
        {
            if (struck || !ValidProgress(progress) || progress < settings.StrikeAt)
            {
                return false;
            }

            struck = true;
            return true;
        }

        internal void Begin()
        {
            struck = false;
            lunged = false;
        }

        internal void Finish() => readyAt = clock.Time + settings.Cooldown;
        internal void Interrupt(float delay) => readyAt = clock.Time + delay;

        internal void Restore(EnemyMeleeState state)
        {
            struck = state.Struck;
            lunged = state.Lunged;
            readyAt = clock.Time + state.Cooldown;
        }

        private static bool ValidProgress(float progress) => float.IsFinite(progress) && progress >= 0;
        private static bool ValidTarget(EnemyMeleeObservation target) => target.IsValid && target.HasDamageableTarget && target.TargetAlive;
    }
}
