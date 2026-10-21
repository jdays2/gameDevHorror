using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>Three explicit roles: domain rules, locomotion and combat; no Unity object references.</summary>
    public sealed class SleeperController
    {
        private readonly SleeperModel model;
        private readonly IEnemyBrainMotor motor;
        private readonly IEnemyBrainMelee melee;

        public SleeperController(SleeperModel model, IEnemyBrainMotor motor, IEnemyBrainMelee melee)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.motor = motor ?? throw new ArgumentNullException(nameof(motor));
            this.melee = melee ?? throw new ArgumentNullException(nameof(melee));
        }

        public SleeperModel Model => model;

        public void Step(float deltaTime, bool targetAlive, bool exposed, bool attacksEnabled, Vector2 at, Vector2 target)
        {
            SleeperIntent intent = model.Step(deltaTime, targetAlive, exposed);
            if (intent == SleeperIntent.Dead)
            {
                return;
            }

            Vector2 velocity = Vector2.zero;
            switch (intent)
            {
                case SleeperIntent.Hunt:
                    if (attacksEnabled && melee.CanStart(at))
                    {
                        var begin = new BeginSleeperAttackCommand(model, (target - at).normalized);
                        if (begin.CanExecute().Success)
                        {
                            melee.Begin();
                            begin.Execute();
                        }
                    }
                    else
                    {
                        velocity = motor.Follow(target, model.PursuitSpeed);
                    }

                    break;
                case SleeperIntent.Attack:
                    velocity = melee.Step(model.AttackProgress, at, model.AttackHeading, true);
                    if (model.AttackProgress >= 1)
                    {
                        var complete = new CompleteSleeperAttackCommand(model);
                        melee.Finish();
                        complete.Execute();
                        velocity = Vector2.zero;
                    }

                    break;
                case SleeperIntent.Stagger:
                    velocity = model.StaggerVelocity;
                    break;
            }

            motor.Move(velocity, intent != SleeperIntent.Stagger && model.State == SleeperState.Hunting);
        }

        public CommandResult Wake() => new WakeSleeperCommand(model).Execute();
        public CommandResult Kill() => new KillSleeperCommand(model).Execute();
        public CommandResult Stagger(Vector2 direction)
        {
            var command = new StaggerSleeperCommand(model, direction);
            CommandResult result = command.CanExecute();
            if (!result.Success)
            {
                return result;
            }

            if (model.State == SleeperState.Attacking)
            {
                melee.Interrupt(model.InterruptDelay);
            }

            return command.Execute();
        }

        public CommandResult Restore(SleeperStateSnapshot state) => new RestoreSleeperCommand(model, state).Execute();
    }
}
