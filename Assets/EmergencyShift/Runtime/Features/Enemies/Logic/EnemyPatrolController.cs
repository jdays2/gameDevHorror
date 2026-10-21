using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class EnemyPatrolController
    {
        private readonly EnemyPatrolModel model;
        public EnemyPatrolController(EnemyPatrolModel model) => this.model = model ?? throw new ArgumentNullException(nameof(model));
        public bool IsInitialized => model.IsInitialized;
        public Vector2 Origin => model.Origin;
        public bool HasTarget => model.HasTarget;
        public Vector2 Target => model.Target;
        public bool IsPausing => model.IsPausing;
        public EnemyPatrolState Capture() => model.Capture();
        public CommandResult Initialize(Vector2 origin) => new InitializeEnemyPatrolCommand(model, origin).Execute();
        public CommandResult Reset() => new ResetEnemyPatrolCommand(model).Execute();
        public CommandResult Restore(EnemyPatrolState state) => new RestoreEnemyPatrolCommand(model, state).Execute();
        public Vector2 Step(EnemyPatrolStep frame) => model.Step(frame);
    }
}
