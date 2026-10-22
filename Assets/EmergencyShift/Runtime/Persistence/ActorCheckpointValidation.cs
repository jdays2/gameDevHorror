using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Raw DTO guards shared by preflight and native restoration. No mutation or scene access.</summary>
    public static class ActorCheckpointValidation
    {
        public static void ValidateStalker(ShadowStalker2D.Snapshot actor, int maximumHealth)
        {
            if (actor == null || actor.melee == null || actor.health < 0 || actor.health > maximumHealth ||
                (actor.health == 0) != (actor.state == ShadowStalker2D.BehaviourState.Dead))
            {
                throw new InvalidOperationException("Invalid Stalker health/melee checkpoint.");
            }

            // Preserve old eyeBurned-only saves. Validate raw new values before any legacy clamping.
            float eyeDamage = actor.eyeBurned ? 1 : 0;
            float burnContact = 0;
            if (actor.hasEyeDamageProgress)
            {
                eyeDamage = actor.eyeDamage;
                burnContact = actor.burnContact;
            }

            var state = new StalkerStateSnapshot((StalkerState)actor.state, actor.aggro, eyeDamage,
                burnContact, actor.age, actor.exposure, actor.closure, actor.escape, actor.approach,
                actor.staggerLeft, actor.push);
            if (!StalkerModel.ValidateSnapshot(state).Success || !ValidMelee(actor.melee))
            {
                throw new InvalidOperationException("Invalid Stalker checkpoint values.");
            }

            if (actor.patrol != null)
            {
                EnemyPatrol2D.Snapshot route = actor.patrol;
                if (!EnemyPatrolModel.ValidateSnapshot(new EnemyPatrolState(route.origin, route.hasTarget,
                    route.target, route.pause, route.stalled, route.previousDistance)).Success)
                {
                    throw new InvalidOperationException("Invalid Stalker checkpoint patrol.");
                }
            }
        }

        public static void ValidateSleeper(Sleeper2D.Snapshot actor, int maximumHealth)
        {
            if (actor == null || actor.melee == null || actor.health < 0 || actor.health > maximumHealth ||
                (actor.health == 0) != (actor.state == Sleeper2D.BehaviourState.Dead) ||
                (actor.state == Sleeper2D.BehaviourState.Sleeping && actor.aggro) ||
                (actor.state != Sleeper2D.BehaviourState.Sleeping && actor.state != Sleeper2D.BehaviourState.Dead && !actor.aggro))
            {
                throw new InvalidOperationException("Invalid Sleeper health/aggro/melee checkpoint.");
            }

            var state = new SleeperStateSnapshot((SleeperState)actor.state, actor.aggro, actor.age,
                actor.exposure, actor.staggerLeft, actor.push, actor.heading);
            if (!SleeperModel.ValidateSnapshot(state).Success || !ValidMelee(actor.melee))
            {
                throw new InvalidOperationException("Invalid Sleeper checkpoint values.");
            }
        }

        public static void ValidateVisualRotation(Quaternion rotation)
        {
            if (!CheckpointValueValidation.IsUnitRotation(rotation))
            {
                throw new InvalidOperationException("Invalid actor checkpoint visual rotation.");
            }
        }

        public static void ValidateAim(Vector2 direction)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(direction.sqrMagnitude))
            {
                throw new InvalidOperationException("Invalid player checkpoint aim direction.");
            }
        }

        private static bool ValidMelee(EnemyMeleeAttack2D.Snapshot state) =>
            EnemyMeleeModel.ValidateSnapshot(new EnemyMeleeState(state.struck, state.lunged, state.cooldown)).Success;
    }
}
