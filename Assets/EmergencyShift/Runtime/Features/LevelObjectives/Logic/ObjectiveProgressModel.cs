using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.LevelObjectives.Config;

namespace EmergencyShift.Features.LevelObjectives.Logic
{
    public sealed class ObjectiveProgressModel
    {
        private ObjectiveRuleSet rules;
        public int Phase { get; private set; }
        public string Text { get; private set; } = "";

        public ObjectiveProgressModel(ObjectiveRuleSet rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        internal void Configure(ObjectiveRuleSet replacement) => rules = replacement;

        internal CommandResult Refresh(IObjectiveFacts facts)
        {
            for (int index = 0; index < rules.Count; index++)
            {
                LevelObjectiveRule rule = rules.At(index);
                if (!ObjectiveRuleMatcher.Matches(rule.Condition, rule.CardId, facts))
                {
                    continue;
                }

                Phase = rule.Phase;
                Text = rule.Text;
                return CommandResult.Ok();
            }

            return CommandResult.Fail("No objective matches the current facts.");
        }
    }
}
