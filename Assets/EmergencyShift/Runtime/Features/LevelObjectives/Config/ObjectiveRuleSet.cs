using System;
using System.Collections.Generic;

namespace EmergencyShift.Features.LevelObjectives.Config
{
    public sealed class ObjectiveRuleSet
    {
        private readonly LevelObjectiveRule[] rules;
        public int Count => rules.Length;
        public LevelObjectiveRule At(int index) => rules[index];

        public ObjectiveRuleSet(IReadOnlyList<LevelObjectiveRule> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            rules = new LevelObjectiveRule[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                LevelObjectiveRule rule = source[index] ?? throw new ArgumentException("Missing objective rule.", nameof(source));
                rule.Validate();
                rules[index] = new LevelObjectiveRule(rule.Condition, rule.CardId, rule.Phase, rule.Text);
            }
        }
    }
}
