using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.LevelObjectives.Config;
using EmergencyShift.Features.LevelObjectives.Logic;
using EmergencyShift.Features.LevelProgress.Logic;

namespace EmergencyShift.Features.LevelProgress
{
    public sealed class LevelProgressFactory
    {
        private readonly IGameClock clock;

        public LevelProgressFactory(IGameClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public LevelRunController CreateRun(ILevelRunEnvironment environment) => new LevelRunController(new LevelRunModel(clock), environment);

        public ObjectiveProgressController CreateObjectives(ObjectiveRuleSet rules, IObjectiveProgressView view) =>
            new ObjectiveProgressController(new ObjectiveProgressModel(rules), view);

        public static ObjectiveRuleSet Snapshot(LevelDefinition definition)
        {
            if (definition == null)
            {
                return new ObjectiveRuleSet(Array.Empty<LevelObjectiveRule>());
            }

            definition.Validate();
            var rules = new LevelObjectiveRule[definition.ObjectiveCount];
            for (int index = 0; index < rules.Length; index++)
            {
                rules[index] = definition.ObjectiveAt(index);
            }

            return new ObjectiveRuleSet(rules);
        }
    }
}
