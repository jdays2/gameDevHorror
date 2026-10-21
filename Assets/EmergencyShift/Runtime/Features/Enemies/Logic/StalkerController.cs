using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class StalkerController : IDisposable
    {
        private readonly StalkerModel model;
        private readonly IStalkerLocomotion movement;
        private readonly IEnemyBrainMelee melee;

        public StalkerController(StalkerModel model, IStalkerLocomotion movement, IEnemyBrainMelee melee)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.movement = movement ?? throw new ArgumentNullException(nameof(movement));
            this.melee = melee ?? throw new ArgumentNullException(nameof(melee));
            model.PatrolResetRequested += movement.ResetPatrol;
        }

        public StalkerModel Model => model;

        public void Step(StalkerObservation frame)
        {
            StalkerIntent intent = model.Step(frame);
            if (intent == StalkerIntent.DirectStop || intent == StalkerIntent.Stagger)
            {
                movement.Move(intent == StalkerIntent.Stagger ? model.StaggerVelocity : Vector2.zero, false);
                return;
            }

            Vector2 velocity = Vector2.zero;
            switch (intent)
            {
                case StalkerIntent.Patrol:
                    velocity = movement.Patrol(frame.Position, frame.BodyRadius, frame.Solids, frame.DeltaTime);
                    break;
                case StalkerIntent.Approach:
                    velocity = movement.ClearDirection(model.Approach) * model.PursuitSpeed;
                    break;
                case StalkerIntent.Retreat:
                    velocity = movement.ClearDirection(model.Escape) * model.RetreatSpeed;
                    break;
                case StalkerIntent.Pursue:
                    if (frame.AttacksEnabled && model.CanBeginAttack && melee.CanStart(frame.Position))
                    {
                        var begin = new BeginStalkerAttackCommand(model, (frame.TargetPosition - frame.Position).normalized);
                        if (begin.Execute().Success)
                        {
                            melee.Begin();
                        }

                        break;
                    }

                    Vector2 goal = model.PursuitCandidate(frame);
                    if (frame.LineOfSight && (!movement.CanStand(goal) || !movement.Clear(frame.TargetPosition, goal)))
                    {
                        goal = frame.TargetPosition;
                    }

                    if (model.ShouldFollow(frame, goal))
                    {
                        velocity = movement.Follow(goal, model.PursuitSpeed);
                    }

                    break;
                case StalkerIntent.Attack:
                    velocity = melee.Step(model.AttackProgress, frame.Position, model.Escape, false);
                    if (model.AttackProgress >= 1)
                    {
                        melee.Finish();
                        new CompleteStalkerAttackCommand(model).Execute();
                        velocity = Vector2.zero;
                    }

                    break;
            }

            movement.Move(velocity, model.State != StalkerState.Attacking);
        }

        public CommandResult Stagger(Vector2 direction, bool targetAlive)
        {
            bool attacking = model.State == StalkerState.Attacking;
            CommandResult result = new StaggerStalkerCommand(model, direction, targetAlive).Execute();
            if (result.Success && attacking)
            {
                melee.Interrupt(model.InterruptDelay);
            }

            return result;
        }

        public CommandResult Kill() => new KillStalkerCommand(model).Execute();
        public CommandResult Restore(StalkerStateSnapshot state) => new RestoreStalkerCommand(model, state).Execute();
        public void Dispose() => model.PatrolResetRequested -= movement.ResetPatrol;
    }
}
