using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class LevelObjectiveRule
    {
        [SerializeField]
        private ObjectiveCondition condition;
        [SerializeField]
        private string cardId;
        [SerializeField, Min(0)]
        private int phase;
        [SerializeField, TextArea]
        private string text;

        public ObjectiveCondition Condition => condition;
        public string CardId => cardId;
        public int Phase => phase;
        public string Text => text;

        public LevelObjectiveRule()
        {
        }

        public LevelObjectiveRule(ObjectiveCondition condition, string cardId, int phase, string text)
        {
            this.condition = condition;
            this.cardId = cardId;
            this.phase = phase;
            this.text = text;
            Validate();
        }

        public void Validate()
        {
            if (condition < ObjectiveCondition.Always || condition > ObjectiveCondition.WeaponMissing || phase < 0 ||
                ((condition == ObjectiveCondition.HasCard || condition == ObjectiveCondition.MissingCard) && string.IsNullOrWhiteSpace(cardId)))
            {
                throw new InvalidOperationException("Invalid level objective rule.");
            }
        }
    }
}
