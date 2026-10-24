using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.ScriptedEncounters.Logic
{
    public sealed class ScriptedRouteController
    {
        private readonly ScriptedRouteModel model;
        private readonly IEnemyBrainMotor motor;

        public ScriptedRouteModel Model => model;

        public ScriptedRouteController(ScriptedRouteModel model, IEnemyBrainMotor motor)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.motor = motor ?? throw new ArgumentNullException(nameof(motor));
        }

        public void Step(float deltaTime, Vector2 position, bool startSatisfied, bool targetAlive, bool canMove, Vector2 staggerVelocity)
        {
            if (!float.IsFinite(deltaTime) || deltaTime <= 0 || !float.IsFinite(position.sqrMagnitude) || !float.IsFinite(staggerVelocity.sqrMagnitude))
            {
                motor.Move(Vector2.zero, false);
                return;
            }

            if (!targetAlive)
            {
                new AbortScriptedRouteCommand(model).Execute();
                motor.Move(Vector2.zero, false);
                return;
            }

            if (startSatisfied && model.State == ScriptedRouteState.WaitingForTrigger)
            {
                new StartScriptedRouteCommand(model, true).Execute();
            }

            model.Step(deltaTime, position, canMove);
            Vector2 velocity = Vector2.zero;
            if (!canMove && model.ControlsActor)
            {
                velocity = staggerVelocity;
            }
            else if (model.IsMoving)
            {
                float speed = Mathf.Min(model.Speed, Vector2.Distance(position, model.Goal) / deltaTime);
                velocity = motor.Follow(model.Goal, speed);
            }

            motor.Move(velocity, model.IsMoving);
        }
    }
}
