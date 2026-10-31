using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion.Logic
{
    /// <summary>Delegates navigation and contact damage to the existing species-independent modules.</summary>
    public sealed class ScorpionController
    {
        private readonly IEnemyBrainMotor motor;
        private readonly IEnemyBrainMelee melee;

        public ScorpionController(ScorpionModel model, IEnemyBrainMotor motor, IEnemyBrainMelee melee)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            this.motor = motor ?? throw new ArgumentNullException(nameof(motor));
            this.melee = melee ?? throw new ArgumentNullException(nameof(melee));
        }

        public ScorpionModel Model { get; }

        public void Step(Vector2 position, Vector2 target, bool targetAlive, float speed, float attackMultiplier, float cycleSeconds = .36f,
            float maximumTurnDegrees = 360, float accelerationStep = float.PositiveInfinity, float brakingStep = float.PositiveInfinity)
        {
            if (!Model.IsAlive || !targetAlive || !Model.HasAggro)
            {
                if (Model.State == ScorpionState.Attacking || Model.State == ScorpionState.Gathering)
                {
                    melee.Interrupt(0);
                    new ScorpionCommand(Model, ScorpionAction.CancelAttack).Execute();
                }
                Model.StopMotion();
                motor.Move(Vector2.zero, false);
                return;
            }
            if (Model.State == ScorpionState.Gathering)
            {
                // A blocked actor finishes a deliberate foot reposition rather
                // than waiting forever for travel through a neighbour.
                Model.AdvanceGather(cycleSeconds);
            }
            if (Model.FinishIfElapsed())
            {
                melee.Step(1, position, Model.Heading, true);
                melee.Finish();
            }
            else if (Model.State == ScorpionState.Charging && melee.CanStart(position))
            {
                if (new ScorpionCommand(Model, ScorpionAction.Attack, target - position).Execute().Success)
                {
                    melee.Begin();
                }
                else
                {
                    new ScorpionCommand(Model, ScorpionAction.Gather).Execute();
                }
            }
            bool attacking = Model.State == ScorpionState.Attacking;
            if (attacking)
            {
                // Shared strike rules resolve LOS/range/arc once. Its lunge velocity is deliberately unused.
                melee.Step(Model.AttackProgress, position, Model.Heading, true);
            }
            bool slowed = attacking || Model.State == ScorpionState.Gathering;
            float moveSpeed = speed * (slowed ? attackMultiplier : 1);
            Vector2 velocity;
            if (attacking)
            {
                // The moving grasp stays aligned with its committed claws until recovery.
                velocity = Model.Heading * moveSpeed;
                Model.CommitMotion(moveSpeed);
            }
            else
            {
                Vector2 route = motor.Follow(target, moveSpeed);
                velocity = Model.Steer(route, maximumTurnDegrees, accelerationStep, brakingStep);
            }
            motor.Move(velocity, false);
        }

        public void Alert() => new ScorpionCommand(Model, ScorpionAction.Alert).Execute();

        public void Suspend()
        {
            if (Model.State == ScorpionState.Attacking || Model.State == ScorpionState.Gathering)
            {
                melee.Interrupt(0);
                new ScorpionCommand(Model, ScorpionAction.CancelAttack).Execute();
            }
            Model.StopMotion();
            motor.Move(Vector2.zero, false);
        }

        public void Kill()
        {
            if (Model.State == ScorpionState.Attacking)
            {
                melee.Interrupt(0);
            }
            new ScorpionCommand(Model, ScorpionAction.Kill).Execute();
            Model.StopMotion();
            motor.Move(Vector2.zero, false);
        }
    }
}
