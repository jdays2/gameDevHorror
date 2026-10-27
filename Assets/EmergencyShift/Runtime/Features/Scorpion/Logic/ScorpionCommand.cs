using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion.Logic
{
    public enum ScorpionAction { Alert, Attack, Kill, CancelAttack, Gather }

    public sealed class ScorpionCommand : ICommand
    {
        private readonly ScorpionModel model;
        private readonly ScorpionAction action;
        private readonly Vector2 heading;

        public ScorpionCommand(ScorpionModel model, ScorpionAction action, Vector2 heading = default)
        {
            this.model = model;
            this.action = action;
            this.heading = heading;
        }

        public CommandResult CanExecute()
        {
            if (model == null || !model.IsAlive || !System.Enum.IsDefined(typeof(ScorpionAction), action))
            {
                return CommandResult.Fail("Scorpion command requires a living actor and known action.");
            }
            if (action == ScorpionAction.Attack &&
                (model.State != ScorpionState.Charging || !model.CrossedGaitBoundary ||
                !float.IsFinite(heading.x) || !float.IsFinite(heading.y) || heading.sqrMagnitude < .001f ||
                Vector2.Angle(model.Facing, heading) > 15))
            {
                return CommandResult.Fail("Attack requires a charging actor, gait boundary and finite heading.");
            }
            if (action == ScorpionAction.Gather && model.State != ScorpionState.Charging)
            {
                return CommandResult.Fail("Only a charging actor can reposition for a grasp.");
            }
            if (action == ScorpionAction.CancelAttack && model.State != ScorpionState.Attacking && model.State != ScorpionState.Gathering)
            {
                return CommandResult.Fail("No active grasp to cancel.");
            }
            return CommandResult.Ok();
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
                case ScorpionAction.Alert:
                    model.Alert();
                    break;
                case ScorpionAction.Attack:
                    model.BeginAttack(heading);
                    break;
                case ScorpionAction.Kill:
                    model.Kill();
                    break;
                case ScorpionAction.CancelAttack:
                    model.CancelAttack();
                    break;
                case ScorpionAction.Gather:
                    model.Gather();
                    break;
            }
            return CommandResult.Ok();
        }
    }
}
