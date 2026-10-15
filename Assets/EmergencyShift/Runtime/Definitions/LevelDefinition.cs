using System;
using UnityEngine;

namespace EmergencyShift
{
    public enum ObjectiveCondition
    {
        Always,
        HasCard,
        MissingCard,
        WeaponMissing
    }

    [Serializable]
    public sealed class LevelObjectiveRule
    {
        public ObjectiveCondition condition;
        public string cardId;
        public int phase;
        [TextArea]
        public string text;
        public bool Matches(PlayerAccessCards2D cards, PlayerWeapon2D weapon)
        {
            switch (condition)
            {
                case ObjectiveCondition.HasCard:
                    return cards.Has(cardId);
                case ObjectiveCondition.MissingCard:
                    return !cards.Has(cardId);
                case ObjectiveCondition.WeaponMissing:
                    return !weapon.HasWeapon;
                default:
                    return true;
            }
        }
    }

    [CreateAssetMenu(menuName = "Emergency Shift/Level definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Min(1)]
        public int saveRevision = 1;
        public string levelId;
        [Tooltip("Project-relative scene path; add the same scene to Build Profiles.")]
        public string scenePath;
        public string exitCardId = "evacuation";
        public string nextLevelId;
        public LevelObjectiveRule[] objectives = Array.Empty<LevelObjectiveRule>();
    }
}
