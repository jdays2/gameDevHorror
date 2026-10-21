using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public sealed class InitializeEnemyPatrolCommand : ICommand
    {
        private readonly EnemyPatrolModel model;
        private readonly Vector2 origin;
        public InitializeEnemyPatrolCommand(EnemyPatrolModel model, Vector2 origin)
        {
            this.model = model;
            this.origin = origin;
        }

        public CommandResult CanExecute() => model != null && EnemyPatrolBounds.Finite(origin)
            ? CommandResult.Ok() : CommandResult.Fail("Patrol needs a model and finite spawn origin.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Initialize(origin);
            }

            return result;
        }
    }
}
