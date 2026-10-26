using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.Logic
{
    public sealed class ClawEnemyController
    {
        private readonly IClawLocomotion movement;
        private readonly IEnemyBrainMelee melee;

        public ClawEnemyController(ClawEnemyModel model, IClawLocomotion movement, IEnemyBrainMelee melee)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            this.movement = movement ?? throw new ArgumentNullException(nameof(movement));
            this.melee = melee ?? throw new ArgumentNullException(nameof(melee));
        }

        public ClawEnemyModel Model { get; }

        public void Step(ClawEnemyObservation frame)
        {
            Vector2 velocity = Vector2.zero;
            ClawEnemyIntent intent = Model.Step(frame);
            switch (intent)
            {
                case ClawEnemyIntent.Patrol:
                    velocity = movement.Patrol(frame);
                    break;
                case ClawEnemyIntent.Pursue:
                    if (frame.Attacks && melee.CanStart(frame.Position))
                    {
                        var command = new ClawEnemyCommand(Model, ClawEnemyAction.BeginAttack, frame.Target - frame.Position);
                        if (command.Execute().Success)
                        {
                            melee.Begin();
                        }
                    }
                    else
                    {
                        velocity = movement.Follow(frame.Target, Model.Speed);
                    }
                    break;
                case ClawEnemyIntent.Attack:
                    velocity = melee.Step(Model.AttackProgress, frame.Position, Model.Heading, true);
                    break;
                case ClawEnemyIntent.FinishAttack:
                    // Resolve a crossed strike even when one slow tick passed the end.
                    melee.Step(1, frame.Position, Model.Heading, true);
                    melee.Finish();
                    break;
                case ClawEnemyIntent.Push:
                    velocity = Model.Push;
                    break;
            }
            movement.Move(velocity, intent != ClawEnemyIntent.Attack && intent != ClawEnemyIntent.Push);
        }

        public void Hit(Vector2 direction)
        {
            bool attacking = Model.State == ClawEnemyState.Attacking;
            if (new ClawEnemyCommand(Model, ClawEnemyAction.Hit, direction).Execute().Success && attacking)
            {
                melee.Interrupt(Model.StaggerSeconds);
            }
        }

        public void Kill()
        {
            new ClawEnemyCommand(Model, ClawEnemyAction.Kill).Execute();
            movement.Move(Vector2.zero, false);
        }
    }
}
