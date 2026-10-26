using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.Logic
{
    public enum ClawEnemyAction { BeginAttack, Hit, Kill }

    public sealed class ClawEnemyCommand : ICommand
    {
        private readonly ClawEnemyModel model;
        private readonly ClawEnemyAction action;
        private readonly Vector2 heading;

        public ClawEnemyCommand(ClawEnemyModel model, ClawEnemyAction action, Vector2 heading = default)
        {
            this.model = model;
            this.action = action;
            this.heading = heading;
        }

        public CommandResult CanExecute()
        {
            if (model == null || !model.IsAlive || !float.IsFinite(heading.x) || !float.IsFinite(heading.y))
            {
                return CommandResult.Fail("Claw action requires a living actor and finite heading.");
            }
            if (action == ClawEnemyAction.BeginAttack && (model.State != ClawEnemyState.Chasing || heading.sqrMagnitude < .001f))
            {
                return CommandResult.Fail("Claw attack requires pursuit and a committed direction.");
            }
            return System.Enum.IsDefined(typeof(ClawEnemyAction), action) ? CommandResult.Ok() : CommandResult.Fail("Unknown claw action.");
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }
            switch (action)
            {
                case ClawEnemyAction.BeginAttack:
                    model.BeginAttack(heading);
                    break;
                case ClawEnemyAction.Hit:
                    model.Hit(heading);
                    break;
                case ClawEnemyAction.Kill:
                    model.Kill();
                    break;
            }
            return CommandResult.Ok();
        }
    }
}
