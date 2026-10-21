using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class EnemyMeleeController
    {
        private readonly EnemyMeleeModel model;
        private readonly IEnemyMeleeEnvironment environment;

        public EnemyMeleeController(EnemyMeleeModel model, IEnemyMeleeEnvironment environment)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public event Action Lunged;
        public event Action<bool> Resolved;

        public bool CanStart(Vector2 at) => Finite(at) && model.IsReady &&
            model.CanStart(environment.Observe(at, Vector2.zero, false)) && environment.HasLineOfSight(at);

        public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading)
        {
            if (!Finite(at) || !Finite(heading) || !model.ValidTime)
            {
                return Vector2.zero;
            }

            if (model.AdvanceLunge(progress))
            {
                Lunged?.Invoke();
            }

            Vector2 velocity = model.IsMoving(progress) ? environment.LungeVelocity(heading, model.LungeSpeed, fixedHeading) : Vector2.zero;
            if (model.AdvanceStrike(progress))
            {
                bool landed = model.CanBite(environment.Observe(at, heading, fixedHeading), fixedHeading) &&
                    environment.HasLineOfSight(at) &&
                    environment.TryDamage(model.Damage, at, heading);
                Resolved?.Invoke(landed);
            }

            return velocity;
        }

        public CommandResult Begin() => new BeginEnemyMeleeCommand(model).Execute();
        public CommandResult Finish() => new FinishEnemyMeleeCommand(model).Execute();
        public CommandResult Interrupt(float delay) => new InterruptEnemyMeleeCommand(model, delay).Execute();
        public EnemyMeleeState Capture() => model.Capture();
        public CommandResult Restore(EnemyMeleeState state) => new RestoreEnemyMeleeCommand(model, state).Execute();

        private static bool Finite(Vector2 point) => float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.sqrMagnitude);
    }
}
